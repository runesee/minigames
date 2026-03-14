using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Common
{
    [RequireComponent(typeof(Animator))]
    public class EndScreenAnimationPlayer : MonoBehaviour
    {
        [SerializeField] private AnimationClip animationClip;

        private PlayableGraph _playableGraph;
        private AnimationClipPlayable _clipPlayable;

        private void Start()
        {
            if (animationClip == null)
            {
                Debug.LogWarning($"[EndScreenAnimationPlayer] No animation clip assigned on {gameObject.name}.", this);
                return;
            }

            _playableGraph = PlayableGraph.Create($"EndScreen_{gameObject.name}");
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _clipPlayable = AnimationClipPlayable.Create(_playableGraph, animationClip);

            var output = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", GetComponent<Animator>());
            output.SetSourcePlayable(_clipPlayable);

            _playableGraph.Play();
        }

        private void Update()
        {
            if (!_playableGraph.IsValid() || !_clipPlayable.IsValid())
                return;

            if (_clipPlayable.GetTime() >= animationClip.length)
                _clipPlayable.SetTime(0.0);
        }

        private void OnDestroy()
        {
            if (_playableGraph.IsValid())
                _playableGraph.Destroy();
        }
    }
}
