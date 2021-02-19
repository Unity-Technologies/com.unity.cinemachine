# Cinemachine

## What is Cinemachine?
Cinemachine is a suite of ‘smart’ procedural modules which allow you to define the shot and
they’ll dynamically follow your direction. Set up shots which track and compose motion in
realtime, like AI camera operators. The procedural nature makes them bug-resistant as they
always work to make the shot based on your direction. They’re great for gameplay, but they’re
also amazingly fast for cutscenes. Change an animation, a vehicle speed, ground terrain -
whatever - and Cinemachine will dynamically make the shot. You can use really telephoto
lenses and not have to update the cutscene if things change.

## Setup
Cinemachine works out of the box with no dependencies other than Unity itself.  
Just install it and you're ready to go.  It's pure c-sharp, fully open-source, 
and the public API has complete XML documentation built right in.

## History
Cinemachine has been in development over a number of years across multiple projects. We’ve
been designing camera systems for almost 20 years and have shipped millions of AAA titles
across numerous genres. The Cinemachine team has an award winning cinematographer and
a senior engineer with heavy math skills. Also, we love this stuff to bits.

## Mission
Our mission with Cinemachine is to build an entirely unified camera system bridging
gameplay, cutscenes, from fully procedural cameras to entirely canned sequences and
everything in between.

## Example Scenes
Please have a look at our example scenes. They are shipped with the package and can be imported
via the Cinemachine menu.

## Forums
We have a busy discussion area on the forums.
https://forum.unity3d.com/forums/cinemachine.136/

## Development

### General
- [Yamato] CI is triggered automatically for branches (one should not normally commit directly to `master`).
- **Be very deliberate if adding new public APIs**, prefer to keep classes private/internal if possible; public APIs cannot be removed without bumping the major version. Remember to document (`///`) all public APIs.
- We want to catch potential API validator errors early on, so set the planned versions preemptively to `package.json`s in `master`, don't forget to add dummy changelog entries. Let's say we're planning to make a minor +0.0.1 bugfix release: we want to prevent making changes that would require a major version bump. For preview packages, breaking changes require a new minor version, for non-preview packages, breaking changes require a new major version. Meaning, it's best to develop in non-preview "mode" and add the preview tag only when making an actual preview release.
- Develop the features for the release(s) using feature branches named like `dev/my-new-feature`, merge to `master` preferably using squashing (if applicable) and rebasing so that we can keep a clean history.

## Making releases

### General
- Ensure that Jira release issues are correct and up to date
- Ensure that all Jira issues have landed in master and are closed (verified by QA)
- Ensure that CHANGELOG.md is up-to-date (issues listed, version number, date)
- Make sure that `package.json` has the correct version number
- In Runtime/Core/CinemachineCore.cs, update the `BaseURL` string to pint to the correct documentation version, and `kVersionString` to reflect the current version
- Publish to the candidates registry
- When CI is green, request promotion on #devs-pkg-promotion 
- Make a release tag on the commit
- Merge `release/x.y` to master

### Verified Releases
- Publish to the candidates registry
- In Unity repo, edit External/PackageManager/Editor/manifest.json and update the Cinemachine entry
- Make a PR with that mod, start the package verification tests
- When CI is green, and PR tests are green, request publication from release-management

[Yamato]: https://yamato.cds.internal.unity3d.com/jobs/245-com.unity.cinemachine
