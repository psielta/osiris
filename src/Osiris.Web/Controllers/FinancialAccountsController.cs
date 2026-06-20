using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Osiris.Application.Common.Csv;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.Categories.Queries.ListCategories;
using Osiris.Application.Features.FinancialAccountMovements.Commands.AnalyzeCsvImport;
using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewCsvImport;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;
using Osiris.Application.Features.FinancialAccounts.Queries.ExportFinancialAccountStatementPdf;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;
using Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountForEdit;
using Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;
using Osiris.Web.Helpers;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("accounts")]
public sealed class FinancialAccountsController : AppController
{
    private const string MovementPrefix = "Movement";

    private const string CsvMappingViewName = "ImportCsvMapping";

    private const long MaxOfxUploadBytes = 5 * 1024 * 1024;

    private readonly IMediator _mediator;

    public FinancialAccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await _mediator.Send(new ListFinancialAccountsQuery(), cancellationToken);
        return View(accounts);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new FinancialAccountFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialAccountFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateFinancialAccountCommand(model.Name, model.Type, model.InitialBalance),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        return View(new FinancialAccountFormViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            InitialBalance = account.InitialBalance
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FinancialAccountFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;

        try
        {
            var result = await _mediator.Send(
                new UpdateFinancialAccountCommand(id, model.Name, model.Type),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Errors.Any(error => error.Code == ResultErrorCodes.NotFound))
                {
                    return NotFound();
                }

                AddResultErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(model);
        }
    }

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveFinancialAccountCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var movement = new ManualMovementFormViewModel { OccurredOn = BrazilDates.Today() };
        var model = await BuildDetailsViewModelAsync(id, movement, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new ExportFinancialAccountStatementPdfQuery(id), cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("{id:guid}/import")]
    public async Task<IActionResult> Import(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        return View(new OfxImportUploadViewModel { AccountId = account.Id, AccountName = account.Name });
    }

    [HttpPost("{id:guid}/import/preview")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxOfxUploadBytes)]
    public async Task<IActionResult> ImportPreview(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        var uploadModel = new OfxImportUploadViewModel { AccountId = account.Id, AccountName = account.Name };

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecione um arquivo OFX para importar.");
            return View(nameof(Import), uploadModel);
        }

        byte[] content;
        await using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, cancellationToken);
            content = stream.ToArray();
        }

        try
        {
            var result = await _mediator.Send(
                new PreviewOfxImportCommand(account.Id, content, file.FileName),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(nameof(Import), uploadModel);
            }

            var categories = await LoadCategoryItemsAsync(cancellationToken);
            return View(nameof(ImportPreview), BuildPreviewViewModel(result.Value!, categories));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(nameof(Import), uploadModel);
        }
    }

    [HttpPost("{id:guid}/import/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportConfirm(
        Guid id,
        OfxImportPreviewViewModel model,
        CancellationToken cancellationToken)
    {
        var selectedLines = model.Lines
            .Where(line => line.Include)
            .Select(line => new ImportOfxLineInput(
                line.ExternalId,
                line.OccurredOn,
                line.Amount,
                line.Type,
                line.Description,
                line.CategoryId))
            .ToList();

        if (selectedLines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecione ao menos um lançamento para importar.");
            return View(nameof(ImportPreview), await RebuildPreviewAsync(model, cancellationToken));
        }

        try
        {
            var result = await _mediator.Send(new ImportOfxStatementCommand(id, selectedLines), cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(nameof(ImportPreview), await RebuildPreviewAsync(model, cancellationToken));
            }

            TempData["ImportSummary"] = BuildSummaryMessage(result.Value!);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(nameof(ImportPreview), await RebuildPreviewAsync(model, cancellationToken));
        }
    }

    [HttpGet("{id:guid}/import/csv")]
    public async Task<IActionResult> ImportCsv(Guid id, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        return View(new OfxImportUploadViewModel { AccountId = account.Id, AccountName = account.Name });
    }

    [HttpPost("{id:guid}/import/csv/analyze")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxOfxUploadBytes)]
    public async Task<IActionResult> ImportCsvAnalyze(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        var account = await _mediator.Send(new GetFinancialAccountForEditQuery(id), cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        var uploadModel = new OfxImportUploadViewModel { AccountId = account.Id, AccountName = account.Name };

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Selecione um arquivo CSV para importar.");
            return View(nameof(ImportCsv), uploadModel);
        }

        byte[] content;
        await using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, cancellationToken);
            content = stream.ToArray();
        }

        try
        {
            var result = await _mediator.Send(
                new AnalyzeCsvImportCommand(account.Id, content, file.FileName),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                return View(nameof(ImportCsv), uploadModel);
            }

            return View(CsvMappingViewName, BuildMappingViewModel(account.Id, account.Name, file.FileName, content, result.Value!));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            return View(nameof(ImportCsv), uploadModel);
        }
    }

    [HttpPost("{id:guid}/import/csv/map")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsvMap(Guid id, CsvImportMappingViewModel model, CancellationToken cancellationToken)
    {
        model.AccountId = id;
        await PopulateSampleAsync(model, cancellationToken);
        return View(CsvMappingViewName, model);
    }

    [HttpPost("{id:guid}/import/csv/preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsvPreview(Guid id, CsvImportMappingViewModel model, CancellationToken cancellationToken)
    {
        model.AccountId = id;
        var content = DecodeBase64(model.FileContentBase64);

        try
        {
            var result = await _mediator.Send(
                new PreviewCsvImportCommand(id, content, model.FileName, model.ToMapping()),
                cancellationToken);

            if (result.IsFailure)
            {
                AddResultErrors(result);
                await PopulateSampleAsync(model, cancellationToken);
                return View(CsvMappingViewName, model);
            }

            var categories = await LoadCategoryItemsAsync(cancellationToken);
            return View(nameof(ImportPreview), BuildPreviewViewModel(result.Value!, categories));
        }
        catch (ValidationException exception)
        {
            AddValidationErrors(exception);
            await PopulateSampleAsync(model, cancellationToken);
            return View(CsvMappingViewName, model);
        }
    }

    [HttpPost("{id:guid}/movements")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovement(
        Guid id,
        ManualMovementFormViewModel movement,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateManualMovementCommand(
                    id,
                    movement.Type,
                    movement.Amount,
                    movement.OccurredOn,
                    movement.Description,
                    movement.CategoryId,
                    movement.Notes),
                cancellationToken);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            AddMovementResultErrors(result);
        }
        catch (ValidationException exception)
        {
            AddMovementValidationErrors(exception);
        }

        var model = await BuildDetailsViewModelAsync(id, movement, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(nameof(Details), model);
    }

    private async Task<FinancialAccountDetailsViewModel?> BuildDetailsViewModelAsync(
        Guid id,
        ManualMovementFormViewModel movement,
        CancellationToken cancellationToken)
    {
        var statement = await _mediator.Send(new GetFinancialAccountDetailsQuery(id), cancellationToken);
        if (statement is null)
        {
            return null;
        }

        var categories = await _mediator.Send(new ListCategoriesQuery(IncludeArchived: false), cancellationToken);
        var categoryItems = categories
            .Select(category => new SelectListItem
            {
                Text = category.Name,
                Value = category.Id.ToString()
            })
            .ToArray();

        return new FinancialAccountDetailsViewModel
        {
            Account = statement,
            Movement = movement,
            Categories = categoryItems
        };
    }

    private async Task<SelectListItem[]> LoadCategoryItemsAsync(CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new ListCategoriesQuery(IncludeArchived: false), cancellationToken);
        return categories
            .Select(category => new SelectListItem { Text = category.Name, Value = category.Id.ToString() })
            .ToArray();
    }

    private static OfxImportPreviewViewModel BuildPreviewViewModel(
        OfxImportPreviewDto preview,
        IReadOnlyCollection<SelectListItem> categories)
    {
        return new OfxImportPreviewViewModel
        {
            AccountId = preview.AccountId,
            AccountName = preview.AccountName,
            BankId = preview.BankId,
            AccountNumber = preview.AccountNumber,
            PeriodStart = preview.PeriodStart,
            PeriodEnd = preview.PeriodEnd,
            TotalCount = preview.TotalCount,
            NewCount = preview.NewCount,
            DuplicateCount = preview.DuplicateCount,
            Categories = categories,
            Lines = preview.Lines
                .Select(line => new OfxImportLineViewModel
                {
                    ExternalId = line.ExternalId,
                    OccurredOn = line.OccurredOn,
                    Amount = line.Amount,
                    Type = line.Type,
                    Description = line.Description,
                    IsDuplicate = line.IsDuplicate,
                    Include = !line.IsDuplicate
                })
                .ToList()
        };
    }

    private async Task<OfxImportPreviewViewModel> RebuildPreviewAsync(
        OfxImportPreviewViewModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await LoadCategoryItemsAsync(cancellationToken);
        model.TotalCount = model.Lines.Count;
        model.DuplicateCount = model.Lines.Count(line => line.IsDuplicate);
        model.NewCount = model.TotalCount - model.DuplicateCount;
        return model;
    }

    private static string BuildSummaryMessage(OfxImportResultDto summary)
    {
        return summary.SkippedDuplicates > 0
            ? $"{summary.Imported} lançamento(s) importado(s). {summary.SkippedDuplicates} ignorado(s) por já existirem."
            : $"{summary.Imported} lançamento(s) importado(s).";
    }

    private static CsvImportMappingViewModel BuildMappingViewModel(
        Guid accountId,
        string accountName,
        string fileName,
        byte[] content,
        CsvAnalysisDto analysis)
    {
        var model = new CsvImportMappingViewModel
        {
            AccountId = accountId,
            AccountName = accountName,
            FileName = fileName,
            FileContentBase64 = Convert.ToBase64String(content),
            Delimiter = analysis.Delimiter,
            Encoding = analysis.Encoding,
            HasHeader = true,
            HeaderLineIndex = analysis.SuggestedHeaderLineIndex,
            SampleRows = analysis.SampleRows
        };

        if (analysis.SavedMapping is { } saved)
        {
            ApplySavedMapping(model, saved);
        }
        else
        {
            var (dateColumn, descriptionColumn, amountColumn) =
                CsvMappingGuesser.Guess(analysis.SampleRows, model.HeaderLineIndex, model.DecimalSeparator);
            model.DateColumn = dateColumn;
            model.DescriptionColumn = descriptionColumn;
            model.AmountColumn = amountColumn;
        }

        return model;
    }

    private static void ApplySavedMapping(CsvImportMappingViewModel model, CsvImportMapping saved)
    {
        model.Delimiter = saved.Delimiter;
        model.Encoding = saved.Encoding;
        model.HasHeader = saved.HasHeader;
        model.HeaderLineIndex = saved.HeaderLineIndex;
        model.AmountMode = saved.AmountMode;
        model.DateColumn = saved.DateColumn;
        model.DescriptionColumn = saved.DescriptionColumn;
        model.SecondaryDescriptionColumn = saved.SecondaryDescriptionColumn;
        model.AmountColumn = saved.AmountColumn;
        model.CreditColumn = saved.CreditColumn;
        model.DebitColumn = saved.DebitColumn;
        model.TypeColumn = saved.TypeColumn;
        model.ExternalIdColumn = saved.ExternalIdColumn;
        model.DateFormat = saved.DateFormat;
        model.DecimalSeparator = saved.DecimalSeparator;
    }

    // Re-reads the sample (with the chosen delimiter/encoding) from the carried bytes so the mapping
    // page can refresh after a change or re-render with errors, preserving the user's selections.
    private async Task PopulateSampleAsync(CsvImportMappingViewModel model, CancellationToken cancellationToken)
    {
        var content = DecodeBase64(model.FileContentBase64);

        try
        {
            var result = await _mediator.Send(
                new AnalyzeCsvImportCommand(model.AccountId, content, model.FileName, model.Delimiter, model.Encoding),
                cancellationToken);

            if (result.IsSuccess)
            {
                model.SampleRows = result.Value!.SampleRows;
                model.AccountName = result.Value!.AccountName;
                model.Delimiter = result.Value!.Delimiter;
                model.Encoding = result.Value!.Encoding;
            }
        }
        catch (ValidationException)
        {
            // Leave the sample as-is; the user's selections are still shown.
        }
    }

    private static byte[] DecodeBase64(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        try
        {
            return Convert.FromBase64String(content);
        }
        catch (FormatException)
        {
            return [];
        }
    }

    private void AddMovementResultErrors(Result result)
    {
        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(error.Field)
                ? string.Empty
                : $"{MovementPrefix}.{NormalizeField(error.Field)}";
            ModelState.AddModelError(key, error.Message);
        }
    }

    private void AddMovementValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(
                $"{MovementPrefix}.{NormalizeField(error.PropertyName)}",
                error.ErrorMessage);
        }
    }
}
