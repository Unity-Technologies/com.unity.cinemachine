# unitypackage-template
Unity Package Template

## Package structure

```
<root>
  ├── package.json
  ├── README.md
  ├── CHANGELOG.md
  ├── LICENSE.md
  ├── QAReport.txt
  ├── PlayModeSample.cs
  ├── Tests
  │   └── PlayModeSampleTest.cs
  ├── Editor
  │   ├── EditorSample.cs
  │   └── Tests
  │       └── EditorSampleTest.cs
  └── Documentation
      ├── ApiDocs
      │   ├── EditorSample.mem.xml
      │   └── PlayModeSample.mem.xml
      └── FeatureDocs
          ├── EditorSample.md
          └── PlayModeSample.md
```

## Step-by-Step development guide

1. Fork `unitypackage-template` git repo
   * Via GitHub: ***[Explain how]***
   * Via Git: ***[Explain how]***
   * Naming convention for repo: `unitypackage-[package name]`
     (Example: `unitypackage-terrain-builder`)

2. Start Unity, create an empty project

3. Thru console, go to Assets folder, clone repo locally
```
cd <YourProjectPath>
git clone git://..... Assets
cd Assets
git status
```
4. Update **package.json** file

   * Required fields:
        * `"name"` is the package name, it should follow this naming convention: `"com.unity3d.[package name]"`
        (Example: `"com.unity3d.terrain-builder"`)
        * `"version"` is the package version, it **must** follow this versioning convention: `x.y.z` where:
            * `x` is the **major** version: 
                If the package implement new stuff that is likely to break the existing API, you need to bump x because it is a major version.
            * `y` is the **minor** version: 
                If you are implementing minor features in a backward-compatible way, or fixing bugs within the development cycle, then you will bump y because this is what’s called a minor version.
            * `z` is the **patch** version: 
                When fixing bugs based on a package that was part of an official unity release. We should bump z.  Patch bump should be reserved by sustainable engineering.

       * `"unity"` is the Unity Version your package is compatible with. (Example: `"2017.3"`)
    
    * Option fields:

        * ***[Check with Packam Team for optional fields]***

5. Update **README.md**

    * ***[Explain what needs to be modified]***

6. Update **LICENSE.md**

    * ***[Explain what needs to be modified]***

7. Update **CHANGELOG.md**

    * ***[Explain what needs to be modified]***

8. If you want to share your project with other developers:

    * ***[Explain how]***

9. If you want to dry-run your package with **upm**:

    * ***[Explain how]***

10. If you want to publish your package on **staging area**:

    * ***[Explain how]***
