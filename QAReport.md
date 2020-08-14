**Cinemachine 2.6.1-preview-5 Testing Report**

- The focus of this testing was mainly to ensure that the bugfixes were tested and verified:

	- Bugfix (1252431): Fixed unnecessary GC Memory allocation every frame when using timeline
	- Bugfix (1260385): check for prefab instances correctly
	- Bugfix (1266191) Clicking on foldout labels in preferences panel toggles their expanded state
	- Bugfix (1266196) Composer target Size label in preferences panel was too big
	
	Regression testing was done around the other bugfixes:
	
	-  PostProcessing/VolumeSettings FocusTracksTarget, StateDrivenCamera, vertical group composition, Cached Scrubbing.

	Also tested Multi-object edit capabilities and Target Offset in Framing Transposer.
	
	All the Cinemachine Example Scene have been checked with Unity 2020.1, 2019.4 and 2018.4.