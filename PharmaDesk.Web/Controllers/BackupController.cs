using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaDesk.Application.Common.Interfaces;
using PharmaDesk.Application.Security;
using PharmaDesk.Web.Models.Backup;
using PharmaDesk.Web.Security;

namespace PharmaDesk.Web.Controllers;

[Authorize]
public class BackupController : Controller
{
    private readonly IBackupService _backupService;
    private readonly IAuditLogService _auditLog;

    public BackupController(IBackupService backupService, IAuditLogService auditLog)
    {
        _backupService = backupService;
        _auditLog = auditLog;
    }

    [PermissionAuthorize(PermissionCodes.Backup.View)]
    public IActionResult Index()
    {
        var backups = _backupService.ListBackups()
            .Select(b => new BackupListItemViewModel
            {
                FileName = b.FileName,
                SizeBytes = b.SizeBytes,
                CreatedAtUtc = b.CreatedAtUtc,
            })
            .ToList();

        return View(backups);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Backup.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBackup()
    {
        try
        {
            var backup = await _backupService.CreateBackupAsync();
            _backupService.ApplyRetention();

            await _auditLog.LogAsync("BackupCreated", "Backup", backup.FileName, $"Created backup ({backup.SizeBytes:N0} bytes)");

            TempData["StatusMessage"] = $"Backup created: {backup.FileName}";
        }
        catch (Exception ex)
        {
            TempData["StatusMessage"] = $"Backup failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Backup.View)]
    public IActionResult Download(string fileName)
    {
        var fullPath = _backupService.GetFullPath(fileName);
        if (fullPath is null)
        {
            return NotFound();
        }

        return PhysicalFile(fullPath, "application/octet-stream", Path.GetFileName(fullPath));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.Backup.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fileName)
    {
        if (_backupService.DeleteBackup(fileName))
        {
            await _auditLog.LogAsync("BackupDeleted", "Backup", fileName);
            TempData["StatusMessage"] = $"Deleted {fileName}.";
        }
        else
        {
            TempData["StatusMessage"] = "That backup file could not be found.";
        }

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize(PermissionCodes.Backup.View)]
    public IActionResult RestoreInstructions(string fileName)
    {
        if (_backupService.GetFullPath(fileName) is null)
        {
            return NotFound();
        }

        var model = new RestoreInstructionsViewModel
        {
            FileName = fileName,
            Script = _backupService.BuildRestoreScript(fileName),
        };

        return View(model);
    }
}
