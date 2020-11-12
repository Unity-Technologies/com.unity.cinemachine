**Cinemachine 2.7 Testing Report**

- The focus of the testing was mainly on two new features in Cinemachine 2.7:
  - CinemachineConfiner2D - improved 2D confiner.
    - Manual testing on different confiner shape and size as well as Cinemachine VCam ortho size. 
  - Virtual Camera Lens can now override physical camera settings (orthographic/perspective, sensor size, gate fit, focal length and FOV)
    - Manual testing of physical camera override using different Cinemachine scenarios and the already available Sample Scenes

- Testing of UI update:
  - Moved Cinemachine menu to GameObject Create menu and Right Click context menu for Hierarchy.
  - Storyboard Global Mute moved from Cinemachine menu to Cinemachine preferences.
- The following Bugfix were verified:
  - Bugfix (1060230) - lens inspector sometimes displayed ortho vs perspective incorrectly for a brief time.
  - Bugfix (1283984) - Error message when loading new scene with DontDestroyOnLoad.
  - Bugfix (1284701) - Edge-case exception when vcam is deleted.
  - Bugfix - long-idle vcams when reawakened sometimes had a single frame with a huge deltaTime.
  - Bugfix - PostProcessing temporarily stopped being applied after exiting play mode.
- Regression testing was done mainly using the Cinemachine Sample Scenes.
