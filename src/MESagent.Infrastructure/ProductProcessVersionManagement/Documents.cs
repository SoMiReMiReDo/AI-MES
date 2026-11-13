using System.Security.Cryptography;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductProcessVersionManagement;

public sealed partial class ManufacturingBackend
{
    private static string DocumentRoot => Environment.GetEnvironmentVariable("MES_DOCUMENT_ROOT")
        ?? Path.Combine(AppContext.BaseDirectory, ".local", "engineering-documents");
    private object DocumentView(EngineeringDocument doc) => new
    {
        doc.Id, doc.EntityType, doc.EntityId, doc.OriginalFilename, doc.Revision,
        doc.Checksum, doc.FileSize, doc.LifecycleStatus, doc.ReleasedAt, etag = Tag(doc.RowVersion)
    };
    private async Task<OrganizationIdentityAccessControlResult> Documents(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && r.Method == "GET")
        {
            var query = db.Set<EngineeringDocument>().Where(x => x.EntityType == "RouteStep");
            if (r.Query.TryGetValue("entityId", out var value) && Guid.TryParse(value, out var entityId))
                query = query.Where(x => x.EntityId == entityId);
            return new(await Page(query, r, DocumentView));
        }
        if (p.Length == 2 && r.Method == "POST")
        {
            Fields(r.Body, ["entityType", "entityId", "revision", "filename", "content"]);
            if (Text(r.Body, "entityType") != "RouteStep")
                throw Error("指导书必须关联路线步骤");
            var stepId = Guid.Parse(Text(r.Body, "entityId"));
            await DraftStep(stepId);
            var bytes = Convert.FromBase64String(Text(r.Body, "content"));
            if (bytes.Length is < 5 or > 10485760 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
                throw Error("仅接受不超过 10 MiB 的 PDF");
            var filename = Path.GetFileName(Text(r.Body, "filename"));
            if (!filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || filename.Length > 200)
                throw Error("PDF 文件名无效");
            var revision = Text(r.Body, "revision");
            if (string.IsNullOrWhiteSpace(revision) || revision.Length > 64)
                throw Error("文档修订号必填且不超过 64 字符");
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            Directory.CreateDirectory(DocumentRoot);
            var path = Path.Combine(DocumentRoot, hash + ".pdf");
            if (!File.Exists(path)) await File.WriteAllBytesAsync(path, bytes);
            var document = new EngineeringDocument
            {
                EntityType = "RouteStep", EntityId = stepId, OriginalFilename = filename,
                DocumentType = "WorkInstruction", MimeType = "application/pdf", FileSize = bytes.Length,
                Revision = revision, Checksum = hash, StoragePath = hash + ".pdf", LifecycleStatus = "Draft",
                    UploadedById = actor.UserId
            };
            db.Add(document);
 Audit(document, "Uploaded");
 await db.SaveChangesAsync();
            return new(DocumentView(document), Tag(document.RowVersion), 201);
        }
        if (p.Length < 3 || !Guid.TryParse(p[2], out var id))
            throw Error("文档不存在", 404);
        var doc = await Get<EngineeringDocument>(id);
        if (doc.EntityType != "RouteStep")
            throw Error("文档不在当前模块范围", 404);
        await Get<RouteStep>(doc.EntityId);
        if (r.Method == "GET" && p.Length == 3)
            return new(DocumentView(doc), Tag(doc.RowVersion));
        var safePath = Path.Combine(DocumentRoot, doc.Checksum + ".pdf");
        if (doc.Checksum.Length != 64 || doc.Checksum.Any(c => !Uri.IsHexDigit(c)))
            throw Error("文档摘要无效");
        if (r.Method == "GET" && p.Length == 4 && p[3] == "content")
        {
            if (!File.Exists(safePath))
                throw Error("文档文件缺失", 409);
            return new(new EngineeringFile(await File.ReadAllBytesAsync(safePath), doc.OriginalFilename));
        }
        if (r.Method != "POST" || p.Length != 4 || p[3] != "release")
            throw Error("接口不存在", 404);
        Match(r.Version, doc.RowVersion);
        await DraftStep(doc.EntityId);
        if (doc.LifecycleStatus != "Draft")
            throw Error("文档已发布", 409);
        if (!File.Exists(safePath) || Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(safePath))) != doc.Checksum)
            throw Error("文件缺失或摘要不匹配", 409);
        doc.LifecycleStatus = "Released";
 doc.ReleasedAt = Now;
 doc.ReleasedById = actor.UserId;
        Audit(doc, "Released");
 await db.SaveChangesAsync();
        return new(DocumentView(doc), Tag(doc.RowVersion));
    }
}
