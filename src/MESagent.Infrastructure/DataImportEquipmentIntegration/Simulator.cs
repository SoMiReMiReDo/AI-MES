using System.Text;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.EquipmentMaintenanceResources;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.DataImportEquipmentIntegration;

internal sealed partial class IntegrationBackend
{
    private const string SimulatorProtocol = "Simulator";
    private const string SimulatorTopic = "simulator";

    private async Task<OrganizationIdentityAccessControlResult> Connections(
        OrganizationIdentityAccessControlRequest r, string[] path)
    {
        var scoped = from connection in db.Set<MachineConnection>()
                     join equipment in EquipmentResources.EquipmentScope(db, fabId)
                         on connection.EquipmentId equals equipment.Id
                     where connection.Protocol == SimulatorProtocol
                     select new { Connection = connection, Equipment = equipment };

        if (path.Length == 2 && r.Method == "GET")
        {
            var rows = scoped.OrderBy(x => x.Equipment.Code).ThenBy(x => x.Connection.Name)
                .ThenBy(x => x.Connection.Id)
                .Select(x => new SimulatorConnectionView(x.Connection.Id, x.Equipment.Id, x.Equipment.Code,
                    x.Connection.Name, x.Connection.Status, x.Connection.LastConnectedAt));
            return new(await Page(rows, r));
        }
        if (path.Length == 3 && r.Method == "GET")
        {
            var id = Id(path[2]);
            var row = await scoped.Where(x => x.Connection.Id == id)
                .Select(x => new SimulatorConnectionView(x.Connection.Id, x.Equipment.Id, x.Equipment.Code,
                    x.Connection.Name, x.Connection.Status, x.Connection.LastConnectedAt))
                .SingleOrDefaultAsync() ?? throw NotFound();
            return new(row);
        }
        if (path.Length == 2 && r.Method == "POST")
        {
            var input = Body<RegisterSimulatorConnection>(r);
            var equipment = await EquipmentResources.ScopedEquipment(db, fabId, input.EquipmentId)
                ?? throw NotFound();
            return await WithReceipt(r, input, () => RegisterConnection(equipment, input.Name));
        }
        if (path.Length is not (4 or 5) || path[3] != "messages")
            throw NotFound();

        var connectionId = Id(path[2]);
        var target = await scoped.SingleOrDefaultAsync(x => x.Connection.Id == connectionId) ?? throw NotFound();
        var messages = db.Set<MachineMessage>().Where(x => x.MachineConnectionId == connectionId
            && x.Topic == SimulatorTopic);
        if (r.Method == "GET" && path.Length == 4)
        {
            var rows = messages.OrderByDescending(x => x.ReceivedAt).ThenByDescending(x => x.Id)
                .Select(x => new SimulatorMessageSummary(x.Id, x.SourceMessageId!, x.ReceivedAt,
                    x.ProcessingStatus, x.EquipmentEventId));
            return new(await Page(rows, r));
        }
        if (r.Method == "GET" && path.Length == 5)
        {
            var message = await messages.SingleOrDefaultAsync(x => x.Id == Id(path[4])) ?? throw NotFound();
            return new(MessageView(message));
        }
        if (r.Method == "POST" && path.Length == 4)
            return await Receive(target.Connection, target.Equipment, Body<ReceiveSimulatorMessage>(r));
        throw NotFound();
    }

    private async Task<OrganizationIdentityAccessControlResult> RegisterConnection(Equipment equipment, string name)
    {
        name = Text(name, 200, "连接名称");
        if (!equipment.IsActive)
            throw Error(409, "EquipmentInactive", "请选择启用的设备");
        if (await db.Set<MachineConnection>().AnyAsync(x => x.EquipmentId == equipment.Id
            && x.Protocol == SimulatorProtocol && x.Name == name))
            throw Error(409, "SimulatorConnectionExists", "此设备已有同名模拟连接");

        var connection = new MachineConnection
        {
            EquipmentId = equipment.Id,
            Name = name,
            Protocol = SimulatorProtocol,
            Status = "Unknown"
        };
        db.Add(connection);
        Audit(nameof(MachineConnection), connection.Id, "SimulatorConnectionRegistered", new { equipment.Id, name });
        return new(new SimulatorConnectionView(connection.Id, equipment.Id, equipment.Code, name, connection.Status,
            null), Status: 201);
    }

    private async Task<OrganizationIdentityAccessControlResult> Receive(
        MachineConnection connection, Equipment equipment, ReceiveSimulatorMessage input)
    {
        var source = Text(input.SourceMessageId, 128, "来源编号");
        var raw = input.RawPayload;
        if (raw == null || raw.Contains('\0'))
            throw Error(422, "InvalidRequest", "原报文必须为有效文本");
        if (Encoding.UTF8.GetByteCount(raw) > 8192)
            throw Error(413, "SimulatorMessageTooLarge", "原报文超过 8 KiB");

        var prior = await db.Set<MachineMessage>().SingleOrDefaultAsync(x => x.MachineConnectionId == connection.Id
            && x.SourceMessageId == source);
        if (prior != null)
        {
            if (prior.Topic != SimulatorTopic || prior.RawPayload != raw)
                throw Error(409, "SourceMessageConflict", "此连接的来源编号已用于不同报文");
            return new(MessageView(prior), Replayed: true);
        }

        var message = new MachineMessage
        {
            MachineConnectionId = connection.Id,
            SourceMessageId = source,
            Topic = SimulatorTopic,
            RawPayload = raw,
            ReceivedAt = Now,
            ProcessingStatus = "Rejected"
        };
        await ProcessMessage(connection, equipment, source, message);
        db.Add(message);
        Audit(nameof(MachineMessage), message.Id, "SimulatorMessageReceived", new
        {
            connectionId = connection.Id,
            equipmentId = equipment.Id,
            message.SourceMessageId,
            message.ProcessingStatus,
            message.EquipmentEventId
        });
        return new(MessageView(message), Status: 201);
    }

    private async Task ProcessMessage(
        MachineConnection connection, Equipment equipment, string source, MachineMessage message)
    {
        if (!connection.IsActive || !equipment.IsActive)
        {
            message.ProcessingError = "模拟连接或设备已停用";
            return;
        }

        SimulatorPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SimulatorPayload>(message.RawPayload, Json);
        }
        catch (JsonException)
        {
            payload = null;
        }
        if (payload?.Kind == null || payload.State == null || payload.Reason == null
            || payload.Kind.Contains('\0') || payload.State.Contains('\0') || payload.Reason.Contains('\0'))
        {
            message.ProcessingError = "报文必须为含 occurredAt、kind、state 和 reason 的 JSON 对象";
            return;
        }

        var parsed = JsonSerializer.Serialize(payload, Json);
        message.ParsedData = parsed;
        var backend = new EquipmentBackend(db, fabId, actor, require, clock);
        var input = new SimulateEquipmentEvent(SimulatorProtocol, source, payload.OccurredAt, payload.Kind,
            payload.State, payload.Reason, payload.ReasonCodeId, payload.ShiftId);
        EquipmentEvent equipmentEvent;
        try
        {
            equipmentEvent = await backend.ApplySimulatedEvent(equipment, input, SimulatorProtocol, source, parsed,
                connection.Id);
        }
        catch (AppException e) when (e.Status == 422)
        {
            message.ProcessingError = e.Message;
            return;
        }
        message.EquipmentEventId = equipmentEvent.Id;
        message.ProcessingStatus = equipmentEvent.ProcessingStatus;
        message.ProcessingError = equipmentEvent.RejectionReason;
        if (equipmentEvent.ProcessingStatus == "Applied" && payload.Kind == "Connection")
        {
            connection.Status = payload.State;
            connection.UpdatedAt = message.ReceivedAt;
            if (payload.State == "Online")
                connection.LastConnectedAt = message.ReceivedAt;
        }
    }

    private static SimulatorMessageView MessageView(MachineMessage message) => new(
        message.Id, message.SourceMessageId!, message.ReceivedAt, message.ProcessingStatus, message.EquipmentEventId,
        message.RawPayload,
        message.ParsedData == null ? null : JsonSerializer.Deserialize<JsonElement>(message.ParsedData),
        message.ProcessingError);
}
