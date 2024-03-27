# Cinemachine Target Group

Use Cinemachine Target Group to treat multiple GameObjects as a single Tracking target. It can also be used as targets for procedural behaviour that needs to know the size of the target, for example the [Group Framing](CinemachineGroupFraming.md) extension.

To create a Target Group:
1. Add a CinemachineTargetGroup component to an empty GameObject.

To create a CinemachineCamera with a Target Group:
1. In the Unity menu, choose __GameObject > Cinemachine > Target Group Camera__. <br/>Unity adds a new CinemachineCamera and Target Group to the Scene. The __Follow__ and __Look At__ targets in the CinemachineCamera refer to the new Target Group.

To convert an existing CinemachineCamera target to a target group:
1. Select _Convert to TargetGroup_ from the popup menu to the right of the Tracking Target field in the CinemachineCamera inspector


To Populate a Target Group:
1. In the [Hierarchy](https://docs.unity3d.com/Manual/Hierarchy.html), select the new Target Group object.
2. In the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html), click the + sign to add a new item to the group.
3. In the new item, assign a GameObject (you can drag and drop from the Hierarchy), and edit the __Weight__ and __Radius__ properties.
4. To add more GameObjects to the Target Group, repeat steps 2-3.

![Cinemachine Target Group with two targets](images/CinemachineTargetGroup.png)

## Properties:

| **Property:** || **Function:** |
|:---|:---|:---|
| __Position Mode__ || How to calculate the position of the Target Group. |
| | _Group Center_ | Use the center of the axis-aligned bounding box that contains all items of the Target Group. |
| | _Group Average_ | Use the weighted average of the positions of the items in the Target Group. |
| __Rotation Mode__ || How to calculate the rotation of the Target Group.  |
| | _Manual_ | Use the values specified in the Rotation properties of the Target Group’s transform. |
| | _Group Average_ | Weighted average of the orientation of the items in the Target Group. |
| __Update Method__ || When to update the transform of the Target Group. |
| | _Update_ | Update in the normal MonoBehaviour Update() method. |
| | _Fixed Update_ | Updated in sync with the Physics module, in FixedUpdate(). |
| | _Late Update_ | Updated in MonoBehaviour `LateUpdate()`. |
| __Targets__ || The list of target GameObjects. |
| | _Weight_ | How much weight to give the item when averaging. This cannot be negative. |
| | _Radius_ | The radius of the item, used for calculating the bounding box. This cannot be negative. |
