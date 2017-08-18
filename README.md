# unitypackage-template

### Unity Package Template

Lorem ipsum dolor sit amet, suspendisse porttitor erat nunc habitasse id, maecenas sagittis feugiat luctus a mattis nulla, turpis nam sagittis semper, ipsum mollis sodales wisi faucibus mauris, suspendisse in leo nostra arcu. Ante integer tincidunt ut condimentum sem praesent, convallis erat adipiscing, maecenas purus fringilla, eu quam eu, at diam laoreet eu sed morbi. In sed aenean vitae, aliquam consectetuer leo ipsum, sollicitudin sit facilisis nibh id, consectetuer purus pede non. 

---
## Package structure

```
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

### Note:
* Package structure will follow special folders from Unity, see https://docs.unity3d.com/Manual/SpecialFolders.html for more details

---
## Developping your package

### Step-by-Step development guide

1. Fork `unitypackage-template` repository

    Forking a repository is a simple two-step process. On GitHub, navigate to the [UnityTech/unitypackage-template](https://github.com/UnityTech/unitypackage-template) repository.
    Fork button is at the top-right corner of the page, click **Fork**.
    That's it! Now, you have a fork of the original `UnityTech/unitypackage-template` repository.

    Naming convention for your repository: `unitypackage-[your package name]`
    (Example: `unitypackage-terrain-builder`)

2. Start **Unity**, create a local empty project. 

    Naming convention proposed for your project: `unitypackage-[your package name]-project`
    (Example: `unitypackage-terrain-builder-project`)

3. Launch console (or terminal) application, go to the newly created project folder, then clone your repository

    ```
    cd <YourProjectPath>
    git clone git@github.com:UnityTech/unitypackage-[your package name].git Assets
    ```

4. Update **package.json** file

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

5. Update **README.md**

    *  ***[TODO]***

6. Update **LICENSE.md**

    *  ***[TODO]***

7. Update **CHANGELOG.md**

    *  Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

---
## Sharing your package

### If you want to share your project with other developers:

On other developer's machine:

2. Start **Unity**, create a local empty project. 

    Naming convention proposed for your project: `unitypackage-[your package name]-project`
    (Example: `unitypackage-terrain-builder-project`)

3. Launch console (or terminal) application, go to the newly created project folder, then clone your repository

    ```
    cd <YourProjectPath>
    git clone git@github.com:UnityTech/unitypackage-[your package name].git Assets
    ```
4. That's it!

### If you want to dry-run your package with **upm**:

---
## Publishing your package

### If you want to publish your package on **staging area**:

**Coming soon**

Sed lacinia elit, ullamcorper aliquam proin auctor, a ullamcorper, ultricies aliquam sed, sed mollis maecenas justo. At viverra, sit id lacus vel curabitur vestibulum, tristique congue eu magna nulla sociis eget, orci dolor. Etiam sem nisl proin in tempor, aliquam ut massa, erat erat quam vel ornare at, justo ac in integer neque condimentum et. Pellentesque enim nulla, id diam, ad nullam pellentesque in, scelerisque leo.
