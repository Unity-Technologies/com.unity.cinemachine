using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    /// Objects of Interest:
    ///
    /// - Component on an individual object that maintains an ‘interest level’
    /// - Registers itself with story manager on creation
    ///   - Notifies story manager when it becomes active; a story thread is created for it
    /// - Players, enemies, doors, interactive elements etc
    /// - Exposes an "interesting" value for the story manager to order.  
    ///   Pushes this value every frame to the corresponding story thread
    /// - Subscribes to actions of the specific object, ie:
    ///   - Taking damage
    ///   - Causing damage
    ///   - Opening / being interacted with (doors / terminals / health packs etc)
    /// - "Interesting" value is a function of actions and other things such as
    ///   - Range to player (for non-player objects)
    ///   - Novelty (new things are often more interesting for a while)
    /// - Can maintain a separate list of ‘object-specific’ cameras that get activated 
    ///     when the object wakes up
    /// 
    public abstract class StoryThreadSubjectBase : MonoBehaviour 
    {
        StoryManager.Subject mSubject;

	    protected void Start() 
        {
            mSubject = new StoryManager.Subject(transform);
	    }

        protected void OnEnable()
        {
            StoryManager.Instance.CreateStoryThread(mSubject);
        }

        protected void OnDisable()
        {
            StoryManager.Instance.DestroyStoryThread(mSubject);
        }

        protected void Update() 
        {
            // Calculate interest level and push it to story thread
            var th = StoryManager.Instance.GetStoryThread(mSubject);
            if (th != null)
            {
                th.InterestLevel = ComputeInterest(th.InterestLevel);
            }
	    }

        // Object-specific interest level.
        // How interesting is this object right now?
        // Story thread will be sorted by interest level.
        // Value is arbitrary, and can be a function of anything you want.
        abstract protected float ComputeInterest(float currentInterestLevel);
    }

}