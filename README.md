# Welcome to the Command Palette (CmdPal) Azure Extension (Preview) repo

This repository contains the source code for:

* [Command Palette Azure Extension (Preview)](https://github.com/microsoft/CmdPalAzureExtension)

Related repositories include:

* [PowerToys Command Palette utility](https://github.com/microsoft/PowerToys/tree/main/src/modules/cmdpal)

## Installing and running Command Palette Azure Extension (Preview)

### Requirements
The Command Palette Azure Extension (Preview) requires:
* PowerToys with Command Palette included (version 0.90.0 or above)
* Windows 11
* An ARM64 or x64 processor

### Command Palette [Recommended]

* In Command Palette, search for "Install Command Palette extensions" and press Enter.
* Search for "Command Palette Azure Extension (Preview)".
* Press enter to download the extension.
* Navigate back to the main screen. If the extension was able to detect your account automatically, you will see the following commands:
    * Saved Azure DevOps queries
    * Saved Azure DevOps pull request searches
    * Saved Azure DevOps pipeline searches
    * Sign out of the Azure extension (Preview)
* Otherwise, you'll see a "Sign in to the Azure extension (Preview)" command. Enter your Azure Dev Ops credentials to get started.

### WinGet

More instructions coming soon!

### Microsoft Store

The Command Palette Azure Extension (Preview) is coming to the Microsoft Store. Stay tuned for updates and instructions.

### Other install methods

#### Via GitHub

For users who are unable to install the Command Palette Azure Extension (Preview) from winget or the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/CmdPalAzureExtension/releases).

---

## Getting started

Check out our [quick start guide](docs/quickstartguide.md) to start using the Azure extension (Preview).

## Contributing

We are excited to work alongside you, our amazing community, to build and enhance the Command Palette Azure Extension (Preview)!

## Communicating with the team

The easiest way to communicate with the team is via GitHub issues.

Please file new issues, feature requests and suggestions, but **DO search for similar open/closed preexisting issues before creating a new issue.**

If you would like to ask a question that you feel doesn't warrant an issue (yet), please reach out to us via X or Bluesky:

* Kayla Cinnamon, Senior Product Manager: [@cinnamon_msft on X](https://twitter.com/cinnamon_msft), [@kaylacinnamon on Bluesky](https://bsky.app/profile/kaylacinnamon.bsky.social)
* Clint Rutkas, Principal Product Manager: [@clintrutkas on X](https://twitter.com/clintrutkas), [@clintrutkas on Bluesky](https://bsky.app/profile/clintrutkas.bsky.social)

## Building the code

* Clone the repository
* Uninstall the Command Palette Azure Extension (Preview) (Command Palette has a hard time choosing which extension to use if two versions exist)
* Open `AzureExtension.sln` in Visual Studio 2022 or later and build from the IDE, or run `build\scripts\Build.ps1` from a Visual Studio command prompt.

## Code of Conduct

We welcome contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.