# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:

**Cinemachine 2.4.0-preview-10 Testing Report**
?
- The focus of this testing was mainly to ensure that the upgrade to 2.4.0 from a previous version was fixing errors printed in the console when under HDRP environment:
  - CM 2.4.0 installed using a 2018.4 HDRP template
  - CM 2.4.0 installed using a 2019.3 HDRP template
  - CM 2.4.0 installed using a 2020.1 HDRP template
    - Note that the provided template in 2019.3 and 2020.1 doesn't installed the latest HDRP version available (HDRP 7.1.6) . The upgrade of the HDRP package (HDRP 7.1.7) having CM 2.4.0 have also been tested.
- Regression testing on overall functionalities
- Example package integrity tested
  - Under HDRP, the material conversion tool provided doesn't convert all the materials
  - When loaded, some scenes complain about the GUI Layer component on the main camera that was removed in 2019.3 which is then remove automatically from the game object.
- Various Bug fixes validated as part of previous 2.4.0 preview versions
- Minor tweak to fix in the changelog about Screen XY Framing Transposer range which should be "-0.5 to 1.5"
- Not covered by this test pass:
  - Under HDRP, PostProcessingStack wasn't covered in an environment where the Volume Setting feature is present.
  - Timeline / Cinemachine track have barely been tested, only through example files we ship with Cinemachine.
