# UPM Package Starter Kit

The purpose of this template is to provide data structure and develpment guidelines of new packages for the **Unity Package Manager (UPM)**.

This is the first of many steps towards an automated package publishing experience within Unity, therefore, this package template starter kit is a fraction of the creation, edition, validation and publishing tools we will end up with.

We hope you enjoy your experience, you can use **#devs-packman** on Slack to provide feedback or ask questions around your package development efforts.

## Are you ready to become a package?
The Package Manager is work in progress for Unity, and in that sense, there are a few criteria that must be met to be considered on the package list at this time:
- **Your code accesses public Unity C# apis only.**  If you have a native code component, it will need to ship with an official editor release.  Internal API access might eventually be possible for Unity made packages, but not for the time being.
- **Your code doesn't require security, obfuscation or conditional access control.**  Anyone will be able to download your package, and even play with the source code.
- **You have no urgent need to release your package.**  Our current target for new packages is aligned with 2018.1, although, based on upcoming package requests and limited packman capacity, that release date is not assured for any package.
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
  │   ├── EditorExample.cs
  │   └── Tests
  │       └── EditorExampleTest.cs
  ├── Runtime
  │   ├── RuntimeExample.cs
  │   └── Tests
  │       └── RuntimeExampleTest.cs
  ├── Samples
  │   └─── SampleExample.cs
  └── Documentation
      ├── ApiDocs
      │   ├── EditorExample.mem.xml
      │   └── RuntimeExample.mem.xml
      └── PackageDocs
          └── your-package-name.md
```

### Note

* Package structure will follow special folders from **Unity**, see [Special folders](https://docs.unity3d.com/Manual/SpecialFolders.html) for more details

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
            * The pacth version (X.Y.**Z**), is reserved for sustainable engineering use only.

        * `"unity"`: Unity Version your package is compatible with. (Example: `"2018.1"`)

        * `"description"`: Description of your package, which will appear in the Package Manager UI.

    * Optional fields:

        * `"dependencies"`: List of packages this package depends on.  All dependencies will also be downloaded and loaded in a project, alongside your package.  Here's an example:
        ```
        dependencies: {
          "com.unity.ads": "1.0.0"
          "com.unity.analytics": "2.0.0"
        }
        ```

        * `"keywords"`: List of words that will be indexed by the package manager search engine to facilitate discovery of the package.

        * `"category"`: List of Unity defined categories used for browsing and filtering (**In Development**)

1. Update **README.md**

    The README.md file should contain all pertinents information for your package developpers like:

    * Prerequistes
    * External tools or development libraries
    * Required installed Software
    * Command lines to build, test, run your package.

1. Rename and Update **your-package-name.md** documentation file

    Use this template to create preliminary, high-level documentation meant to introduce users to the feature and the sample files included in this package.

1. Update **CHANGELOG.md**

    * Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).


## Register your package
If you think you are working on a feature that could make a good package candidate please take a minute to answer a few questions [Here](https://docs.google.com/forms/d/e/1FAIpQLSedxgDcIyf1oPyhWegp5FBvMm63MGAopeJhHDT5bU_BkFPNIQ/viewform?usp=sf_link) in  order to share the following info:
* Package Name
* Dev owner
* QA owner
* Unity Version Target
* Short Package Description.

Working with the board of dev directors and with product management, we will schedule the entry of the candidates in the ecosystem, based on technical challenges and on our feature roadmap.
Don’t hesitate to reach out and join us on **#devs-packman** on Slack.

## Share your package

If you want to share your project with other developers, steps are similar to what's done above.  On the other developer's machine:

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

    The staging repository is monitored by QA and release management, and is where package validation will take place before it is accepted in production.  To publish to staging, follow the following steps:
      * Join the **#devs-packman** channel on Slack, and request a staging **USERNAME** and **API_KEY**.
      * [Install NodeJs](https://nodejs.org/en/download/), so you can have access to **npm** (Node Package Manager).
      * If developing on Windows, [install Curl](https://curl.haxx.se/download.html).  Curl already comes with Mac.
      * Setup your credentions for **npm** by running the following command line from the root folder of your package.
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
      "registry": "http://staging-packages.unity.com"
      ```

## Getting your package published to Production

  Packages are promoted to the **production** repository from **staging**, described above.  Certain criteria must be met before submitting a request to promote a package to production.
  The list of criteria can be found [here](https://docs.google.com/forms/d/e/1FAIpQLSdSIRO6s6_gM-BxXbDtdzIej-Hhk-3n68xSyC2sM8tp7413mw/viewform)

  Release Management requires the following to promote a package to **production**:
  1. Submit one or more Test Project(s) in Ono, so that your new package can be tested in all ABVs moving forward.

      * Create a branch in Ono, based on the latest branch this package must be compatible with (trunk, or release branch)
      * If your package contains **EditorTests**:
        * In in [root]\Tests\EditorTests, create a new EditorTest Project (For new packages) or use an existing project (For new versions of existing package).
      * If your package contains **PlaymodeTests**:
        * In in [root]\Tests\PlaymodeTests, create a new PlaymodeTest Project (For new packages) or use an existing project (For new versions of existing package).
      * Modify the project’s manifest.json file to include the staging version of the package (name@version).
      * Commit your branch changes to Ono, and run all Windows & Mac Editor/PlayMode tests (not full ABV) in Katana.
      * Once the tests are green on Katana, create a PR with the changed manifest and add `Latest Release Manager` as a reviewer
  2. Make sure you’ve taken care of all checklist items on the package publishing form, and [Submit your package publishing request to Release Management](https://docs.google.com/forms/d/e/1FAIpQLSdSIRO6s6_gM-BxXbDtdzIej-Hhk-3n68xSyC2sM8tp7413mw/viewform).

**At this point release management will validate your package content, and check that the editor tests are passed before promoting the package to production.**

Once the package is in production, one more PR is required to complete your package publishing:
1. In your existing branch, change the test project manifest to point to your production package by removing the following line ``"registry": "http://staging-packages.unity.com"``
2. If your package is meant to ship with a release of the editor (default package), follow these steps:
      * Modify the in editor manifest (_[root]\External\PackageManager\Editor\Manifest.json_) to include your package in the list of dependencies.
      * Create a PR update with the editor manifest changes, add both `Latest Release Manager` and  `Trunk Merge Queue` as reviewers.
      * Sit back, relax, your work is done!
      
