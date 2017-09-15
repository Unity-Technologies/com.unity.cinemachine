### **_Package Documentation Template_**

*Use this template to create preliminary, high-level documentation meant to introduce users to the feature and the sample files included in this package. When writing your documentation, do the following:*

1. *Follow instructions in italics.*

2. *Replace angle brackets with the appropriate text. For example, replace "&lt;package name&gt;" with the official name of the package.*

3. *Delete sections that do not apply to your package. For example, a package with sample files does not have a "Using &lt;package_name&gt;" section, so this section can be removed.*

4. *After documentation is completed, make sure you delete all instructions and examples in italic (including this preamble).*

# About &lt;package name&gt;

*Name the heading of the first topic after the official name of the package. Check with your Product Manager to ensure that the package is named correctly.*

*This first topic includes a brief, high-level explanation of the package and, if applicable, provides links to Unity Manual topics.*

*There are two types of packages: a) packages that include features that augment the Unity Editor, or b) packages that include sample files. Choose one of the following introductory paragraphs that best fits the package:*

*a)*
Use the &lt;package name&gt; package to &lt;list of the main uses for the package&gt;. For example, use &lt;package name&gt; to create/generate/extend/capture &lt;mention major use case, or a good example of what the package can be used for&gt;. The &lt;package name&gt; package also includes &lt;other relevant features or uses&gt;.

*or*

*b)*
The &lt;package name&gt; package includes examples of &lt;name of asset type, model, prefabs, and/or other GameObjects in the package&gt;. For more information, see &lt;xref to topic in the Unity Manual&gt;.

*Examples (For reference. Do not include in the final documentation file):*

*a)*
*Use the Unity Recorder package to capture and save in-game data. For example, use Unity Recorder to record an mp4 file during a game session. The Unity Recorder package also includes an interface for setting-up and triggering recording sessions.*

*b)*
*The Timeline Examples package includes examples of Timeline assets, Timeline Instances, animation, GameObjects, and scripts that illustrate how to use Unity's Timeline. For more information, see [ Unity's Timeline](https://docs.unity3d.com/Manual/TimelineSection.html) in the [Unity Manual](https://docs.unity3d.com). For licensing and usage, see Package Licensing.*

## Requirements

*This subtopic includes a bullet list with the compatible versions of Unity. This subtopic may also include additional requirements or recommendations. An example includes a dependency on other packages.*

This &lt;package name&gt; version &lt;package version&gt; is compatible with the following versions of the Unity Editor:

* 2017.2 and later (recommended)
* 2017.1
* 5.6

To use this package, you must have the following packages installed:

* &lt;package name with link to Github repository&gt;
* &lt;package name with link to Github repository&gt;
* &lt;package name with link to Github repository&gt;

## Known Limitations

*This section lists the known limitations with this version of the package.*If there are no known limitations, or if the limitations are trivial, exclude this section.** An example is provided:*

The &lt;package name&gt; version &lt;package version&gt; includes the following known limitations:

* &lt;brief one-line description of first limitation.&gt;
* &lt;brief one-line description of second limitation.&gt;
* &lt;and so on&gt;

*Example (For reference. Do not include in the final documentation file):*

*The Unity Recorder version 1.0 has the following limitations:*

* *The Unity Recorder does not support sound.*
* *The Recorder window and Recorder properties are not available in standalone players.*
* *MP4 encoding is only available on Windows.*

# Installing &lt;package name&gt;

*This section should always begin with a cross-reference to the official Unity Manual topic on how to install packages. If the package requires special installation instructions, include these steps in this section.*

&lt;The text and cross-reference is still to be determined. It will be added by the Documentation Team.&gt;

# Package Contents

*This subtopic includes a table with an alphabetical list of filenames and a brief description of each file. If the package includes folders with many motion files, assets, models, or other example files, you can include the name of the parent folder with a generic description for all files within the folder. Examples of file and folder descriptions are included in the table below. Use these examples as a basis for the descriptions of files and folders in your package.*

The following table provides an alphabetical list of the important files and folders included in this package.

|Folder or Filename|Description|
|---|---|
|license.pdf|Document with licensing and copyright information on the contents of the package.<br>Consult this document before using the package contents in your projects and scenes.|
|motion files|Folder containing cleaned motion capture files for animating humanoid characters.|
|shaders|Folder containing different materials, shaders, and lighting examples.|

# Using &lt;package name&gt;

*Exactly what is included in this section depends on the type of package.*

*a)*
*For packages that augment the Unity Editor with additional features, this section should include wor**kflow and/or reference documentation:*

* *At a minimum, this section should include reference documentation that describes the windows, editors, and properties that the package adds to Unity. This reference documentation should include screen grabs, a list of settings, an explanation of what each setting does, and the default values of each setting.*
* *Ideally, this section should also include a workflow: a list of steps that the user can easily follow that demonstrates how to use the feature. This list of steps should include screen grabs to better describe how to use the feature.*

*b)*
*For packages that include sample files, this section may include detailed information on how the user can use these sample files in their projects and scenes. If this section is too repetitive compared to the Package Contents section, do not include this section.*

# Document Revision History

*This section includes the revision history of the document. The revision history tracks when a document is created, edited, and updated. If you create or update a document, you must add a new row describing the revision.  The Documentation Team also uses this table to track when a document is edited and its editing level. An example is provided:*

|Date|Reason|
|---|---|
|Sept 12, 2017|Unedited. Published to package.|
|Sept 10, 2017|Document updated for package version 1.1.New features: <li>audio support for capturing MP4s.<li>Instructions on saving Recorder prefabs|
|Sept 5, 2017|Limited edit by Documentation Team. Published to package.|
|Aug 25, 2017|Document created. Matches package version 1.0.|

