# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:

Cinemachine **2.3.4** (on Unity **2019.1** and **2019.2.0b3**)

- Regression testing on overall functionalities
- Example package integrity tested
- Bug fixes validated
  - BlendList improvements
  - Collider improvements
  - FreeLook guildline display fixes
  - Framing Transposer Center on Activate
  - Blend Interuption Cut fix
  - Fogbugz defect validated and closed:
    - [1150847] Cinemachine brain cut even doesn't trigger when cutting to a camera blend
    - [1138263] Two sets of frame guidelines are drawn when FreeLook camera is used
    - [1119028] [Cinemachine] Using RemoveMember() sets the last member of the Target Group to null
- PostProcessing HDRP Extension Tested (VolumeSettings)
- General Performance improvement untested but no regression was found

