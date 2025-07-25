# Command Palette Azure Extension (Preview) Quick Start Guide

Welcome to the Command Palette Azure Extension (Preview) Quick Start Guide! Below are the instructions to get started.

## Signing in

By default, the Command Palette Azure Extension (Preview) will attempt to log you in automatically with the authenticated Windows account on the device. To determine whether the automatic sign in was successful, open Command Palette (`Windows`+`Alt`+`Space` or your custom keyboard shortcut) and search "azure" in the search bar.

If you were signed in successfully, you'll see the below commands:

![A screenshot of the Command Palette with "azure" in the search bar. Four commands have a blue box around them: "Saved Azure DevOps queries", "Saved Azure DevOps pull request searches", "Saved Azure DevOps pipelines", and "Sign out of the Azure extension"](assets/logged_in_commands.png)

If you were not signed in automatically, you can click the "Sign in" command shown here:

![A screenshot of the Command Palette with "azure" in the search bar. One command has a blue box around it: "Sign into the Azure extension"](assets/sign_in_command.png)

To change the Azure DevOps account the extension uses, sign out of the extension and sign in to the account you'd like.


## Adding searches

To add your favorite queries, pull request searches, and pipelines accessible to Command Palette, follow these steps:

* Select the type of search you want to add (query, pull requests, pipeline)
* Navigate to the Saved Azure DevOps [Search] command, then select "Add a [Search]"

   ![A screenshot of the Azure Extension (Preview) Saved Azure DevOps queries page. The "Add a query" command is highlighted.](assets/add_a_query.png)

* Fill out the form in the Add page:
   ![A screenshot of the "Save query" page.](assets/save_query_page.png)

   Here's an overview of what you'll need:

   * **URL:** The URL to the search you want to save:
      * Queries: the query URL in the address bar
      * Pull request searches: the URL to the repository whose pull requests you want to view
      * Pipelines: the pipeline definition URL (the page with all runs)
      > Note: Temporary queries are currently not supported. To save a query, use the query URL in the address bar of the browser, not the URL from the "Copy query URL" button in Azure DevOps.
   * **Display name:** The name for the search in the Command Palette extension. This can help differentiate "Assigned to me" queries in multiple projects, for example. Each form also shows the default display name for each search type.
   * **View (pull request searches only):** The kinds of pull requests you'd like to see for the given repository.
   * **Pin [Search] to the top level:** Select this checkbox if you want to access the item from the top level (the same place you found the "Sign out of the Azure extension" command). Every saved search can be found under the "Saved Azure DevOps [Search]" command.

* Press Save [Search]
* You can now find your saved search by navigating back (either by clicking on the back arrow or bringing the cursor back to the search bar and pressing `Esc`)

## Viewing a search

Once you saved a search, you can find it in the "Saved Azure DevOps [Search]" command:

![The "Saved Azure DevOps queries" list command results page with two entries: "Add a query" and "Your saved query here!"](assets/saved_query_example.png)

You can select the saved search to view the relevant results from Azure DevOps.

With each subitem, you can open the context menu (`Ctrl`+`K` or click the three little dots in the bottom right corner) to see the actions you can perform with each item. Some examples include:
* Opening the item link in your browser
* Copying ID and URL
* Viewing the status or type of item

## Troubleshooting

If you're not seeing the extension when searching for "azure" or facing another issue, you can reload your Command Palette extensions by typing in "reload" and selecting the "Reload Command Palette extensions" command:

![A screenshot of the Command Palette with "reload" in the search bar. The command "Reload Command Palette extensions" is highlighted](assets/reload_command.png)

If that doesn't help, please [file an issue](https://github.com/microsoft/CmdPalAzureExtension/issues/new).

You can find the extension logs in ```%localappdata%\Packages\Microsoft.CmdPalAzureExtension_8wekyb3d8bbwe\TempState```.