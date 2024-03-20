# Follow and frame a group

Use Cinemachine Target Group to treat multiple Transforms as a single Tracking target.

It can also be used as targets for procedural behaviours that need to know the size of the target, for example the [Group Framing](CinemachineGroupFraming.md) extension.

## To create a Target Group

* Add a CinemachineTargetGroup component to an empty GameObject.

## To create a CinemachineCamera with a Target Group

* In the Unity menu, choose **GameObject** > **Cinemachine** > **Targeted Cameras** > **Target Group Camera**.

  Unity adds a new CinemachineCamera and Target Group to the Scene. The **Follow** and **Look At** targets in the CinemachineCamera refer to the new Target Group.

## To convert an existing CinemachineCamera target to a target group

* Select _Convert to TargetGroup_ from the popup menu to the right of the Tracking Target field in the CinemachineCamera inspector


## To Populate a Target Group

1. In the [Hierarchy](https://docs.unity3d.com/Manual/Hierarchy.html), select the new Target Group object.

2. In the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html), click the + sign to add a new item to the group.

3. In the new item, assign a GameObject (you can drag and drop from the Hierarchy), and edit the **Weight** and **Radius** properties.

4. To add more GameObjects to the Target Group, repeat steps 2-3.

![Cinemachine Target Group with two targets](images/CinemachineTargetGroup.png)
