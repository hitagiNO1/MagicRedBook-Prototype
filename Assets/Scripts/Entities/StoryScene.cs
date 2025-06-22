using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "NewStoryScene", menuName ="Data/New Story Scene")]
[System.Serializable]
public class StoryScene : GameScene
{
    public bool StopMove;
    public List<Sentence> sentences;
    public GameScene nextScene;

    [System.Serializable]
    public struct Sentence
    {
        public string text;
        public Speaker speaker;
        public List<Action> actions;
        public List<TimeLine> timline;

        [System.Serializable]
        public struct Action
        {
            public Speaker speaker;
            public int spriteIndex;
            public Type actionType;
            public Vector2 coords;
            public bool Right;


            [System.Serializable]
            public enum Type
            {
                NONE, APPEAR, DISAPPEAR, MOVE, HIDE, SHOW
            }
        }
        [System.Serializable]
        public struct TimeLine
        {
            public bool animation;
            public PlayableAsset myPlayableAsset;
        }
    }
}

public class GameScene : ScriptableObject { }
