using Spectre.Console;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Graph.Models.ODataErrors;
using TestExternalConnector.Graph;
using TestExternalConnector;
using TestExternalConnector.Data;

var settings = Settings.LoadSettings();

// Initialize Graph
InitializeGraph(settings);
ExternalConnection? currentConnection = null;

var choice = -1;
while (choice != 0)
{
    string description;
    choice = GetSelectedMenuOption(out description);

    AnsiConsole.MarkupLine($"You selected [green]{description} ({choice})[/]");

    switch (choice)
    {
        case 0:
            AnsiConsole.MarkupLine("Goodbye!");
            break;
        case 1:
            currentConnection = await CreateConnectionAsync();
            break;
        case 2:
            currentConnection = await SelectExistingConnectionAsync();
            break;
        case 3:
            currentConnection = await UpdateConnectionAsync();
            break;
        case 4:
            await DeleteCurrentConnectionAsync(currentConnection);
            break;
        case 5:
            await RegisterSchemaAsync();
            break;
        case 6:
            await GetSchemaAsync();
            break;
        case 7:
            await UpdateItemsFromFile("data.csv", settings.TenantId);
            break;
        default:
            AnsiConsole.MarkupLine("[red]Invalid option[/]");
            break;
    }
}

static int GetSelectedMenuOption(out string description)
{
    return GetSelectedOption(out description, [
        "0. Exit",
        "1. Create a connection",
        "2. Select an existing connection",
        "3. Update current connection",
        "4. Delete current connection",
        "5. Register schema for current connection",
        "6. View schema for current connection",
        "7. Push file contents to current connection",
    ]);
}

static int GetSelectedOption(out string description, string[] options)
{
    var selection = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Choose an option:")
            .PageSize(10)
            .AddChoices(options));

    description = selection.Split('.')[1].Trim();
    return int.Parse(selection.Split('.')[0]);
}

static void InitializeGraph(Settings settings)
{
    try
    {
        GraphHelper.Initialize(settings);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error initializing Graph: {ex.Message}");
    }
}

async Task<ExternalConnection?> UpdateConnectionAsync()
{
    if (currentConnection == null)
    {
        Console.WriteLine("No connection selected. Please create a new connection or select an existing connection.");
        return null;
    }

    var connectionName = PromptForInput(
        "Enter a new name for the connection", true) ?? "ConnectionName";
    var connectionDescription = PromptForInput(
        "Enter a new description for the connection", false);

    try
    {
        // Update the connection
        await GraphHelper.UpdateConnectionAsync(
            currentConnection.Id!, connectionName, connectionDescription);
        Console.WriteLine($"Connection updated - Name: {connectionName}");
        
        currentConnection.Name = connectionName;
        currentConnection.Description = connectionDescription;
        return currentConnection;
    }
    catch (ODataError odataError)
    {
        Console.WriteLine($"Error updating connection: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
        return null;
    }
}

async Task<ExternalConnection?> CreateConnectionAsync()
{
    var connectionId = PromptForInput(
        "Enter a unique ID for the new connection (3-32 characters)", true) ?? "ConnectionId";
    var connectionName = PromptForInput(
        "Enter a name for the new connection", true) ?? "ConnectionName";
    var connectionDescription = PromptForInput(
        "Enter a description for the new connection", false);

    try
    {
        // Create the connection
        var connection = await GraphHelper.CreateConnectionAsync(
            connectionId, connectionName, connectionDescription);
        Console.WriteLine($"New connection created - Name: {connection?.Name}, Id: {connection?.Id}");
        return connection;
    }
    catch (ODataError odataError)
    {
        Console.WriteLine($"Error creating connection: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
        return null;
    }
}

static string PromptForInput(string prompt, bool valueRequired)
{
    var promptSetup = new TextPrompt<string>(prompt).PromptStyle("green");

    if (valueRequired)
    {
        const string ValidationMessage = "[red]You must provide a value[/]";
        return AnsiConsole.Prompt(
                promptSetup
                .ValidationErrorMessage(ValidationMessage)
                .Validate(input =>
                {
                    return !string.IsNullOrWhiteSpace(input)
                        ? ValidationResult.Success()
                        : ValidationResult.Error(ValidationMessage);
                }));
    }
    else
    {
        return AnsiConsole.Prompt(promptSetup);
    }
}

// static DateTime GetLastUploadTime()
// {
//     if (File.Exists("lastuploadtime.bin"))
//     {
//         return DateTime.Parse(
//             File.ReadAllText("lastuploadtime.bin")).ToUniversalTime();
//     }

//     return DateTime.MinValue;
// }

static void SaveLastUploadTime(DateTime uploadTime)
{
    File.WriteAllText("lastuploadtime.bin", uploadTime.ToString("u"));
}

async Task<ExternalConnection?> SelectExistingConnectionAsync()
{
    Console.WriteLine("Getting existing connections...");
    try
    {
        var response = await GraphHelper.GetExistingConnectionsAsync();
        var connections = response?.Value ?? new List<ExternalConnection>();
        if (connections.Count <= 0)
        {
            Console.WriteLine("No connections exist. Please create a new connection");
            return null;
        }

        var index = 1;
        var connectionOptions = connections
            .Select(connection => string.Format("{0}. {1}", index++, connection.Name))
            .ToArray();

        var selectedConnection = GetSelectedOption(out var description, connectionOptions);
        ExternalConnection selection = connections[selectedConnection - 1];
        Console.WriteLine($"Selected connection: {selection.Name}");
        return selection;
    }
    catch (ODataError odataError)
    {
        Console.WriteLine($"Error getting connections: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
        return null;
    }
}

async Task DeleteCurrentConnectionAsync(ExternalConnection? connection)
{
    if (connection == null)
    {
        Console.WriteLine(
            "No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    try
    {
        await GraphHelper.DeleteConnectionAsync(connection.Id);
        Console.WriteLine($"{connection.Name} deleted successfully.");
    }
    catch (ODataError odataError)
    {
        Console.WriteLine($"Error deleting connection: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
    }
}

async Task RegisterSchemaAsync()
{
    if (currentConnection == null)
    {
        Console.WriteLine("No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    Console.WriteLine("Registering schema, this may take a moment...");

    try
    {
        // Create the schema
        var schema = SubjectMatter.GenerateSchema();
        await GraphHelper.RegisterSchemaAsync(currentConnection.Id, schema);
        Console.WriteLine("Schema registered successfully");
    }
    catch (ServiceException serviceException)
    {
        Console.WriteLine($"Error registering schema: {serviceException.ResponseStatusCode} {serviceException.Message}");
    }
    catch (ODataError odataError)
    {
        Console.WriteLine($"Error registering schema: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
    }
}

async Task GetSchemaAsync()
{
    if (currentConnection == null)
    {
        Console.WriteLine("No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    try
    {
        var schema = await GraphHelper.GetSchemaAsync(currentConnection.Id);
        Console.WriteLine(JsonSerializer.Serialize(schema));

    }
    catch (ODataError odataError)
    {
        Console.WriteLine($"Error getting schema: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
    }
}

async Task UpdateItemsFromFile(string fileName, string? tenantId)
{
    if (currentConnection == null)
    {
        Console.WriteLine("No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    _ = tenantId ?? throw new ArgumentException("tenantId is null");

    var data = CsvDataLoader.LoadFromCsv(fileName);
    
    Console.WriteLine($"Processing {data.Count} add/updates");
    var success = true;

    var newUploadTime = DateTime.UtcNow;

    foreach (var item in data)
    {
        var newItem = new ExternalItem
        {
            Id = item.Id,
            Content = new ExternalItemContent
            {
                Type = ExternalItemContentType.Text,
                Value = item.Description
            },
            Acl = new List<Acl>
            {
                new Acl
                {
                    AccessType = AccessType.Grant,
                    Type = AclType.Everyone,
                    Value = tenantId,
                }
            },
            Properties = item.AsExternalItemProperties(),
        };

        try
        {
            Console.Write($"Uploading item {item.Title}...");
            await GraphHelper.AddOrUpdateItemAsync(currentConnection.Id, newItem);
            Console.WriteLine("DONE");
        }
        catch (ODataError odataError)
        {
            success = false;
            Console.WriteLine("FAILED");
            Console.WriteLine($"Error: {odataError.ResponseStatusCode}: {odataError.Error?.Code} {odataError.Error?.Message}");
        }
    }

    if (success)
    {
        SaveLastUploadTime(newUploadTime);
    }
}

