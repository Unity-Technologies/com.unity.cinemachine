using System;
using Cinemachine.Examples;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class ActiveControls : MonoBehaviour
{
    public SimplePlayerController PlayerController;

    void Start()
    {
        if (PlayerController == null)
            return;
        
        PlayerController.enabled = false;
        var playableDirector = GetComponent<PlayableDirector>();
        playableDirector.stopped += _ =>
        {
            PlayerController.enabled = true;
        };
    }
}
