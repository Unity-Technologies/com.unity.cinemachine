# Installation and upgrade

Cinemachine is a free package, available for any project.

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui.html).

> [!TIP]
> Once the installation is complete, a new **GameObject** > **Cinemachine** menu is available to add [pre-built Cinemachine Cameras](ui-ref-pre-built-cameras.md) according to your needs.

## Installation requirements

This version of Cinemachine is compatible with the following versions of the Unity Editor:

* 2022.3 LTS and later

Cinemachine has few external dependencies. Just install it and start using it. If you are also using the Post Processing via HDRP or URP volumes, then adapter modules are provided - protected by `ifdef` directives which auto-define if the presence of the dependencies is detected.  

There are similar `ifdef`-protected behaviours for other packages, such as Timeline and UGUI.

## Cinemachine project upgrade

If you have a project that uses an earlier version of Cinemachine and you need to update it to use the latest Cinemachine version, refer to the links in the table below.

> [!CAUTION]
> The Cinemachine 3.x architecture includes many breaking changes compared to Cinemachine 2.x and earlier versions. While it is possible to upgrade an existing project from Cinemachine 2.x to Cinemachine 3.x, you should think carefully about whether you are willing to put in the work.

| Section | Description |
| :--- | :--- |
| [Upgrade your project from Cinemachine 2.x](CinemachineUpgradeFrom2.md) | Instructions to follow if your project currently uses Cinemachine 2.x. |
| [Upgrade from the Asset Store version of Cinemachine](CinemachineUpgradeFromAssetStore.md) | Instructions to follow if your project currently uses a former version of Cinemachine from the Asset Store (prior to version 2.x). |

## Additional resources

* [What has changed in the API between Cinemachine 2.x and 3.x](whats-new.md#major-api-changes)

