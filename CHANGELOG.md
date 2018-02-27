# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.1.13-alpha.1] - 2018-02-27
### Impulse Module
- New Cinemachine Impulse module for event-driven camera shakes

## [2.1.12] - 2018-02-26
### Storyboard, Bugfixes and other enhancements.  Also some restructuring for Package Manager
- Project restructure: Removed Base, Timeline, and PostFX folders from project root.  PostProcessing code must now be manually imported from Cinemachine menu.  No more dependencies on scripting defines.
- New Storyboard extension, to display images over the vcams.  Comes with a Waveform monitor window for color grading
- New option to specify vcam position blend style: linear, spherical, or cylindrical, based on LookAt target
- Added API to support seamless position warping of target objects: OnTargetObjectWarped().
- Added support for custom blend curves
- Lookahead: added Ignore Y Axis Movement option
- Added support for cascading blends (i.e. blending from mid-blend looks better)
- POV/Orbital/FreeLook axis: exposed Min, Max, and Wrap in the UI, for customized axis range
- FreeLook: added Y Axis recentering
- POV: Added recentering feature to both axes
- Path: Added Normalized Path units option: 0 is start of path, 1 is end.
- Path: added length display in inspector
- Timeline Clip Editor: vcam sections are now collapsible
- API enhancement: added Finalize to Pipeline stages, called even for manager-style vcams
- Bugfix: PostProcessing V2 DoF blending works better
- Bugfix: OrbitalTransposer works better with WorldUp overrides
- Bugfix: Remove StateDrivenCamera "not playing a controller" warning
- Bugfix: Handle exceptions thrown by assemblies that don't want to be introspected
- Bugfix: vcams following physics objects incorrectly snapped to origin after exiting play mode
- Bugfix: predictor now supports time pause
- Bugfix: Moved StartCoroutine in Brain to OnEnable()
- Bugfix: Collider was causing problems in Physics on Android platforms
- Bugfix: dragging a vcam's position updtaes prefabs properly
- Bugfix: All extension now respect the "enabled" checkbox
- Bugfix: Undo for Extasion add will no longer generate null references

## [2.1.10] - 2017-11-28
### This is the first UPM release of *Unity Package Cinemachine*.
- New Aim component: Same As Follow Target simply uses the same orientation as the Follow target
- Perlin Noise component: added inspector UI to clone or locate existing Noise profiles, and to create new ones
- Noise Presets were moved outside of the Examples folder
- Example Assets are now included as embedded package, not imported by default
- Bugfix: FreeLook with PositionDelta was not properly updating the heading
- Bugfix: Transitioning between FreeLooks simetimes caused a short camera freeze
- Bugfix: Added some null checks to FreeLook, to prevent error messages at build time

## [2.1.9] - 2017-11-17
### Initial version.
*Version 2.1.9 cloned from private development repository, corresponding to package released on the asset store*
