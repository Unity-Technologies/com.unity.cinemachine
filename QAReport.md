# Quality Report
Use this file to outline the test strategy for this package.

## QA Owner: [*Joel Fortin*]
## UX Owner: [*Joel Fortin*]

## Testing coverage done on this package:
**Regression Pass** :
- Examples scene package integrity tested
- All base cinemachine camera type tested

**New 2.2.8 functionalities** :

- Camera Offset extension tested
- New framing mode behavior in Group Composer tested
- Various optimization accross different components

**Fixes in Cinemachine 2.2.8** :

- Timeline glitches for single frame track
- Paste Component Values works on prefabs
- Nested prefabs support
- Group Composer 3D gizmos drawing fixed
- Dolly then Zoom in Groupe Composer now takes Max Dolly In/Out in consideration
- Update issue in scene view when Fixed update used on Brain now fixed
- Free Look Y axis position fixes when the value is manually typed in the field
- Presets are taken when creating a Cinemachine camera from the menu (if set in the preset manager)