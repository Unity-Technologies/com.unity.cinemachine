# Set up Timeline with Cinemachine Cameras

## Create a Timeline with a Cinemachine Track

1. Create an empty GameObject in your Scene by choosing the **GameObject** > **Create Empty** menu item.
2. Give the empty GameObject a descriptive name. For example, `IntroTimeline`.
3. In your Scene, select your empty Timeline object as the focus to create a Timeline Asset and instance.
4. Click the padlock button to lock the TImeline window to make it easier to add and adjust tracks.
5. Drag a Unity camera with a CinemachineBrain component onto the Timeline Editor, then choose **Create Cinemachine Track** from the drop-down menu.
6. Add other tracks to the Timeline for controlling the subjects of your Scene.  For example, add an Animation track to animate your main character.

**Tip**: Delete the default track that refers to your Timeline object. This track isn’t necessary for Timeline. For example, in the Timeline editor, right-click the track for IntroTimeline and choose **Delete**.

## Add Cinemachine Shot Clips to the Cinemachine Track

1. In the Cinemachine Track, right-click and choose **Add Cinemachine Shot Clip**.
2. Do one of the following:
    * To add an existing CinemachineCamera to the shot clip, drag and drop it onto the CinemachineCamera property in the Cinemachine Shot component.
    * To create a new CinemachineCamera and add it to the shot clip, click Create in the Cinemachine Shot component.
3. In the Timeline editor, adjust the order, duration, cutting, and blending of the shot clip.
4. [Adjust the properties of the CinemachineCamera](CinemachineCamera.md) to place it in the Scene and specify what to aim at or follow.
5. To animate properties of the CinemachineCamera, create an Animation Track for it and animate as you would any other GameObject.
6. Organize your Timeline tracks to fine-tune your Scene.
