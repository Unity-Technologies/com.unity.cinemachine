# UPM Package Starter Kit

The purpose of this package template starter kit is to provide the data structure and develpment guidelines for new packages meant for the **Unity Package Manager (UPM)**.

This is the first of many steps towards an automated package publishing experience within Unity. This package template starter kit is merely a fraction of the creation, edition, validation, and publishing tools that we will end up with.

We hope you enjoy your experience. You can use **#devs-packman** on Slack to provide feedback or ask questions regarding your package development efforts.

## Are you ready to become a package?
The Package Manager is a work-in-progress for Unity and, in that sense, there are a few criteria that must be met for your package to be considered on the package list at this time:
- **Your code accesses public Unity C# apis only.**  If you have a native code component, it will need to ship with an official editor release.  Internal API access might eventually be possible for Unity made packages, but not at this time.
- **Your code doesn't require security, obfuscation, or conditional access control.**  Anyone should be able to download your package and access the source code.
- **You have no urgent need to release your package.**  Our current target for new packages is aligned with 2018.1. Although, based on upcoming package requests and limited packman capacity, that release date is not assured for any package.
- **You are willing to bleed with us a little!** Packman is still in development, and therefore has a few rough edges that will require patience and workarounds.

## Package structure

```none
<root>
  ├── package.json
  ├── README.md
  ├── CHANGELOG.md
  ├── LICENSE.md
  ├── QAReport.md
  ├── Editor
  │   └── EditorExample.cs
  ├── Runtime
  │   └── RuntimeExample.cs
  ├── Tests
  │   ├── Editor
  │   │   └── EditorExampleTest.cs
  │   └── Runtime
  │       └── RuntimeExampleTest.cs
  ├── Samples
  │   └── SampleExample.cs
  └── Documentation
      └── your-package-name.md
```

### Note

Package structure follows special folders from **Unity**, see [Special folders](https://docs.unity3d.com/Manual/SpecialFolders.html) for more details.

## Develop your package
Package development works best within the Unity Editor.  Here's how to set that up:

1. Fork the `upm-package-template` repository

    Forking a repository is a simple two-step process. On GitHub, navigate to the [UnityTech/upm-package-template](https://github.com/UnityTech/upm-package-template) repository.
    Click on the **Fork** button at the top-right corner of the page, and follow the instructions.
    That's it! You now have your own copy (fork) of the original `UnityTech/upm-package-template` repository you can use to develop your package.

    Naming convention for your repository: `upm-package-[your package name]`
    (Example: `upm-package-terrain-builder`)

1. Start **Unity**, create a local empty project.

    Naming convention proposed for your project: `upm-package-[your package name]-project`
    (Example: `upm-package-terrain-builder-project`)

1. In a console (or terminal) application, go to the newly created project folder, then clone your newly forked repository into the packages directory.

    ```none
    cd <YourProjectPath>/packages
    git clone git@github.com:UnityTech/upm-package-[your package name].git [your package name]
    ```

1. Enable package support in the editor (*Internal Feature*).  From the **Project** window's right hang menu, enable `DEVELOPER`->`Show Packages in Project Window` (*only available in developer builds*).  You should now see your package in the Project Window, along with all other available packages for your project.

1. Fill in your package information in file **package.json**

    * Required fields:
        * `"name"`: Package name, it should follow this naming convention: `"com.unity.[your package name]"`
        (Example: `"com.unity.terrain-builder"`)
        * `"version"`: Package version `"X.Y.Z"`, your project **must** adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

            Follow this guideline:

            * To introduce a new feature or bug fix, increment the minor version (X.**Y**.Z)
            * To introduce a breaking API change, increment the major version (**X**.Y.Z)
            * The patch version (X.Y.**Z**), is reserved for sustainable engineering use only.

        * `"unity"`: Unity Version your package is compatible with. (Example: `"2018.1"`)

        * `"description"`: Description of your package. This appears in the Package Manager UI.

    * Optional fields:

        * `"dependencies"`: List of packages this package depends on.  All dependencies will also be downloaded and loaded in a project with your package.  Here's an example:
        ```
        dependencies: {
          "com.unity.ads": "1.0.0"
          "com.unity.analytics": "2.0.0"
        }
        ```

        * `"keywords"`: List of words that will be indexed by the package manager search engine to facilitate discovery.

        * `"category"`: List of Unity defined categories used for browsing and filtering (**In Development**)

1. Update **README.md**

    The README.md file should contain all pertinent information for developers using your package, such as:

    * Prerequistes
    * External tools or development libraries
    * Required installed Software
    * Command line examples to build, test, and run your package.

1. Rename and update **your-package-name.md** documentation file.

    Use this template to create preliminary, high-level documentation. This document is meant to introduce users to the features and sample files included in your package.

1.  Document your package

    ###### Document your public APIs

    * All public APIs need to be documented with XmlDoc.  If you dont need an API to be accessed by clients, mark it as internal instead.
    * API documentation is generated from [XmlDoc tags](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments) included with all public APIs found in the package. See [Editor/EditorExample.cs](https://github.com/UnityTech/upm-package-template/blob/master/Editor/EditorExample.cs) for an example.

    ###### Document your features

    * All packages that expose UI in the editor or runtime features should use the documentation template in [Documentation/your-package-name.md](Documentation/your-package-name.md).

    ###### Documentation flow

    * Documentation needs to be ready when a publish request is sent to Release Management, as they will ask the documentation team to review it.
        * The package will remain in `preview` mode until the final documentation is completed.  Users will have access to the developer-generated documentation only in preview packages.
        * When the documentation is completed, the documentation team will update the package git repo with the updates and they will publish it on the web.
        * The package's development team will then need to submit a new package version with updated docs.

1. Update **CHANGELOG.md**

    Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format, see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).


## Register your package
If you think you are working on a feature that is a good package candidate, please take a minute to fill-in this form: https://docs.google.com/forms/d/e/1FAIpQLSedxgDcIyf1oPyhWegp5FBvMm63MGAopeJhHDT5bU_BkFPNIQ/viewform?usp=sf_link. 

Working with the board of dev directors and with product management, we will schedule the entry of the candidates in the ecosystem, based on technical challenges and on our feature roadmap.
Don’t hesitate to reach out and join us on **#devs-packman** on Slack.

## Share your package

If you want to share your project with other developers, the steps are similar to what's presented above. On the other developer's machine:

1. Start **Unity**, create a local empty project.

    Naming convention proposed for your project: `upm-package-[your package name]-project`
    (Example: `upm-package-terrain-builder-project`)

1. Launch console (or terminal) application, go to the newly created project folder, then clone your repository in the packages directory

    ```none
    cd <YourProjectPath>/packages
    git clone git@github.com:UnityTech/upm-package-[your package name].git [your-package-name]
    ```

## Dry-Run your package with **UPM**

There are a few steps to publishing your package so it can be include as part of the editor's package manifest, and downloaded by the editor.

1. Publishing your changes to the package manager's **staging** repository.

    The staging repository is monitored by QA and release management, and is where package validation will take place before it is accepted in production. To publish to staging, do the follow:
      * Join the **#devs-packman** channel on Slack, and request a staging **USERNAME** and **API_KEY**.
      * [Install NodeJs](https://nodejs.org/en/download/), so you can have access to **npm** (Node Package Manager).
      * If developing on Windows, [install Curl](https://curl.haxx.se/download.html).  Note: Curl already comes with Mac OS.
      * Setup your credentials for **npm** by running the following command line from the root folder of your package.
        ```
        curl -u<USERNAME>@unity:<API_KEY> https://staging-packages.unity.com/auth > .npmrc
        ```
      * You are now ready to publish your package to staging with the following command line, from the root folder of your folder:
      ```none
      npm publish
      ```
2. Test your package locally

    Now that your package is published on the package manager's **staging** repository, you can test your package in the editor by creating a new project, and editing the project's `manifest.json` file to point to your staging package, as such:
      ```
      dependencies: {
        "com.unity.[your package name]": "0.1.0"
      },
      "registry": "https://staging-packages.unity.com"
      ```

## Getting your package published to Production

  Packages are promoted to the **production** repository from **staging**, described above. Certain criteria must be met before submitting a request to promote a package to production.
  The list of criteria can be found [here](https://docs.google.com/forms/d/e/1FAIpQLSdSIRO6s6_gM-BxXbDtdzIej-Hhk-3n68xSyC2sM8tp7413mw/viewform)

  Release Management requires the following to promote a package to **production**:
  1. Submit one or more Test Project(s) in Ono, so that your new package can be tested in all ABVs moving forward.

      * Create a branch in Ono, based on the latest branch this package must be compatible with (trunk, or release branch)
      * If your package contains **EditorTests**:
        * In ``[root]\Tests\EditorTests``, create a new EditorTest Project (for new packages use **YourPackageName**) or use an existing project (for new versions of existing package).
        * A skeleton of EditorTest Project can be found [here](https://oc.unity3d.com/index.php/s/UYYsGINte9Wg6FO). 
        * Modify the project’s manifest.json file to include the staging version of the package (name@version).
        * Your project's manifest.json file should contain the following line ``"registry": "http://staging-packages.unity.com"``.
      * If your package contains **PlaymodeTests**:
        * In ``[root]\Tests\PlaymodeTests``, create a new PlaymodeTest Project (for new packages use **YourPackageName**) or use an existing project (for new versions of existing package).
        * Modify the project’s manifest.json file to include the staging version of the package (name@version).
        * Your project's manifest.json file should contain the following line ``"registry": "http://staging-packages.unity.com"``.
      * Commit your branch changes to Ono, and run all Windows & Mac Editor/PlayMode tests (not full ABV) in Katana.
      * Once the tests are green on Katana, create a PR with the changed manifest and add `Latest Release Manager` as a reviewer
  2. Make sure you’ve completed all checklist items on the package publishing form, and [Submit your package publishing request to Release Management](https://docs.google.com/forms/d/e/1FAIpQLSdSIRO6s6_gM-BxXbDtdzIej-Hhk-3n68xSyC2sM8tp7413mw/viewform).

**At this point release management will validate your package content, and check that the editor/playmode tests are passed before promoting the package to production.**

You will receive a confirmation email once the package is in production. Then, one more step is required to complete package publishing:
1. In your existing branch, change the EditorTest/PlaymodeTest project manifest to point to your production package by removing the following line ``"registry": "https://staging-packages.unity.com"``
2. If your package is meant to ship with a release of the editor (default packages), follow these steps:
      * Get the latest version of your package tarball from production by running the following command line from the folder ``[root]\External\PackageManager\Editor``:  
      ```
      npm pack --registry https://packages.unity.com com.unity.[your package name]
      ```
      * Modify the editor manifest ``[root]\External\PackageManager\Editor\Manifest.json`` to include your package in the list of dependencies.
      * Update your PR, add both `Latest Release Manager` and  `Trunk Merge Queue` as reviewers.
