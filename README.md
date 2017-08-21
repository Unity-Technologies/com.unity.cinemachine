# unitypackage-template

## Synopsis

The purpose of this package template is to provide data structure, samples and guidelines for developping and publishing new packages to developers of new packages for **UPM**.

## Package structure

```none
<root>
  ├── package.json
  ├── README.md
  ├── CHANGELOG.md
  ├── LICENSE.md
  ├── QAReport.txt
  ├── Samples
  │   ├── PlayModeExample.cs
  │   ├── Tests
  │   │   └── PlayModeExampleTest.cs
  │   └── Editor
  │       ├── EditorExample.cs
  │       └── Tests
  │           └── EditorExampleTest.cs
  └── Documentation
      ├── ApiDocs
      │   ├── EditorExample.mem.xml
      │   └── PlayModeExample.mem.xml
      └── FeatureDocs
          ├── EditorExample.md
          └── PlayModeExample.md
```

### Note

* Package structure will follow special folders from **Unity**, see [Special folders](https://docs.unity3d.com/Manual/SpecialFolders.html) for more details

## Developping your package

### Step-by-Step development guide

1. Fork `unitypackage-template` repository

    Forking a repository is a simple two-step process. On GitHub, navigate to the [UnityTech/unitypackage-template](https://github.com/UnityTech/unitypackage-template) repository.
    Fork button is at the top-right corner of the page, click **Fork**.
    That's it! Now, you have a fork of the original `UnityTech/unitypackage-template` repository.

    Naming convention for your repository: `unitypackage-[your package name]`
    (Example: `unitypackage-terrain-builder`)

1. Start **Unity**, create a local empty project. 

    Naming convention proposed for your project: `unitypackage-[your package name]-project`
    (Example: `unitypackage-terrain-builder-project`)

1. Launch console (or terminal) application, go to the newly created project folder, then clone your repository

    ```none
    cd <YourProjectPath>
    git clone git@github.com:UnityTech/unitypackage-[your package name].git Assets
    ```

1. Update **package.json** file

    * Required fields:
        * `"name"` is the package name, it should follow this naming convention: `"com.unity3d.[your package name]"`
        (Example: `"com.unity3d.terrain-builder"`)
        * `"version"` is the package version `"X.Y.Z"`, your project **must** adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

            Follow this guideline:

            If a change introduce... | Number to increment
            --- | ---
            a bug fix | pacth version (X.Y.**Z**)
            a new feature | minor version (X.**Y**.Z)
            a breaking change | major version (**X**.Y.Z)

            Only increment the version number if necessary.

        * `"unity"` is the Unity Version your package is compatible with. (Example: `"2017.3"`)

        * `"description"` is the brief description of your package

    * Optional fields:

        * `"dependencies"` is ***[TODO]***

        * `"keywords"` is ***[TODO]***

        * `"category"` is ***[TODO]***

1. Update **README.md**

    README.md file should contain following section:

    ### **Synopsis**
    At the top of the file there should be a short introduction and/ or overview that explains what the project is. This description should match descriptions added for package managers (Gemspec, package.json, etc.)

    ### **Code Example** *(optional)*
    Show what the library does as concisely as possible, developers should be able to figure out how your project solves their problem by looking at the code example. Make sure the API you are showing off is obvious, and that your code is short and concise.

    ### **Installation** *(optional)*
    Provide code examples and explanations of how to get the project.

    ### **API Reference**
    Depending on the size of the project, if it is small and simple enough the reference docs can be added to the README. For medium size to larger projects it is important to at least provide a link to where the API reference docs live.

    ### **Tests**
    Describe and show how to run the tests with code examples.

    ### **Contributors** *(optional)*
    Let people know how they can dive into the project, include important links to things like issue trackers, irc, twitter accounts if applicable.

1. Update **CHANGELOG.md**

    * Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## Sharing your package

### If you want to share your project with other developers

On other developer's machine:

1. Start **Unity**, create a local empty project. 

    Naming convention proposed for your project: `unitypackage-[your package name]-project`
    (Example: `unitypackage-terrain-builder-project`)

1. Launch console (or terminal) application, go to the newly created project folder, then clone your repository

    ```none
    cd <YourProjectPath>
    git clone git@github.com:UnityTech/unitypackage-[your package name].git Assets
    ```
1. That's it!

### If you want to dry-run your package with **UPM**

*Will come soon*

## Publishing your package

*Will come soon*

### If you want to publish your package on **staging area**

*Will come soon*
