using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[AddComponentMenu("XR/Target Filters/Prefer Marked Grab Targets Filter")]
public sealed class PreferMarkedGrabTargetsFilter : XRBaseTargetFilter
{
    [SerializeField]
    [Tooltip("If enabled, only XRGrabInteractables are reordered. Other interactables are appended afterward.")]
    private bool grabInteractablesOnly = true;

    [SerializeField]
    [Tooltip("If enabled, unmarked targets are still included with default priority 0.")]
    private bool includeUnmarkedTargets = true;

    private readonly List<ScoredTarget> _sortableTargets = new List<ScoredTarget>();
    private readonly List<IXRInteractable> _deferredTargets = new List<IXRInteractable>();

    public override bool canProcess => isActiveAndEnabled;

    public override void Process(IXRInteractor interactor, List<IXRInteractable> targets, List<IXRInteractable> results)
    {
        results.Clear();

        if (!canProcess || targets == null)
            return;

        _sortableTargets.Clear();
        _deferredTargets.Clear();

        for (int i = 0; i < targets.Count; i++)
        {
            IXRInteractable target = targets[i];
            if (target == null)
                continue;

            if (grabInteractablesOnly && target is not XRGrabInteractable)
            {
                _deferredTargets.Add(target);
                continue;
            }

            if (target is not Component component)
            {
                _deferredTargets.Add(target);
                continue;
            }

            PreferredGrabTarget preferred = component.GetComponent<PreferredGrabTarget>();

            if (preferred != null)
            {
                _sortableTargets.Add(new ScoredTarget(target, preferred.Priority, i));
            }
            else if (includeUnmarkedTargets)
            {
                _sortableTargets.Add(new ScoredTarget(target, 0, i));
            }
        }

        _sortableTargets.Sort(CompareTargets);

        for (int i = 0; i < _sortableTargets.Count; i++)
            results.Add(_sortableTargets[i].Target);

        for (int i = 0; i < _deferredTargets.Count; i++)
            results.Add(_deferredTargets[i]);
    }

    private static int CompareTargets(ScoredTarget a, ScoredTarget b)
    {
        int priorityCompare = b.Priority.CompareTo(a.Priority);
        if (priorityCompare != 0)
            return priorityCompare;

        return a.OriginalIndex.CompareTo(b.OriginalIndex);
    }

    private readonly struct ScoredTarget
    {
        public IXRInteractable Target { get; }
        public int Priority { get; }
        public int OriginalIndex { get; }

        public ScoredTarget(IXRInteractable target, int priority, int originalIndex)
        {
            Target = target;
            Priority = priority;
            OriginalIndex = originalIndex;
        }
    }
}