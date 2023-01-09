# Cinemachine
Smart camera tools for passionate creators

## About Cinemachine

Cinemachine is a suite of modules for operating the Unity camera. Cinemachine solves the complex mathematics and logic of tracking targets, composing, blending, and cutting between shots. It is designed to significantly reduce the number of time-consuming manual manipulations and script revisions that take place during development.

## Branching Strategy 

Cinemachine default branch is main and that’s where the latest version of Cinemachine lives. 
For all other minor versions of Cinemachine, a new branch is created under release (e.g. release/2.9, release/2.8). These branches are maintenance branches. 

During development, PRs are merged directly on main and maintenance branches. Those branches should always be shippable: the code compiles, features are complete and the CI pipeline is green.

Once a release is done, a branch with the version name is created under releases (e.g. releases/2.9.4, releases/3.0.0-pre.1). No changes are made to those branches. They exist for reference only. 

## Release Procedure

Cinemachine is a supported package from Unity, and as such, can be installed from the official registry in the package manager of the Unity editor. The [Cinemachine Release Procedure](https://docs.google.com/document/d/13K512E28risGGqodOOE3Pb9w5pLmjLhxGzm5r31TuGE/edit?usp=sharing) is an internal document that contains different information to help with the release of a new version of Cinemachine.
