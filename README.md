This repo implements a simple M365 Graph external connector based on the sample here:
https://learn.microsoft.com/en-us/graph/connecting-external-content-build-quickstart

To try it out, clone the repo, open the folder in VSCode, and open it in the
container. The source code is in the `src/test-external-connector` folder.
You can run it with `dotnet run`.

## Goals
The goal of this Graph connector is to load a data set into the Graph that describes subject matters (or topics)
that a user can consult an external application about. The data set contains the description and keywords of the
subject matter, as well as the related URL to the external application. After this data is loaded into the Graph,
it can be configured as a vertical in the Microsoft Search experience, and enabled for Copilot to surface in chats.

## How to load data into the Graph

- Update the `src/test-external-connector/data.csv` file with the information you want to upload to the graph.
  The data schema is:
    - `id`: The unique identifier for the row (Guid in the sample data, but it can be any string)
    - `title`: The title of the topic.
    - `description`: A short description of the topic that will be indexed in the search index.
    - `keywords`: A comma-separated list of keywords that will be indexed in the search index.
    - `url`: The URL to the external application that the user can consult about the topic.
- Run the app with `dotnet run`
- Select the `Create a connection` option, or, if you already have done so, select `Select an existing connection`
  - If you select `Create a connection`, you will be asked to provide the connection's ID, name, and description.
    See a sample description in the `src/test-external-connector/sample-connection.txt` file. Even though the documentation does not
    mention specific size limits for the description, it seems to be limited to around 500 characters, or you will
    get a 400 error.
- Select `Push file contents to current connection`.

## How to surface the data in Microsoft Search and M365 Copilot
Follow the guidance in the resouces below.

## Resources

- [Microsoft Graph connectors overview for Microsoft Search](https://learn.microsoft.com/en-us/microsoftsearch/connectors-overview)
- [Microsoft Graph Connectors for Microsoft 365 Copilot](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-graph-connector)