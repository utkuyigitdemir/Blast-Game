using UnityEngine;
using BlastGame.Core;
using BlastGame.Data;

namespace BlastGame.Items
{
    public class ItemFactory : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private GameObject rocketPrefab;
        [SerializeField] private GameObject boxPrefab;
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private GameObject vasePrefab;

        [Header("Cube Default Sprites")]
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Sprite greenSprite;
        [SerializeField] private Sprite yellowSprite;

        [Header("Cube Rocket Hint Sprites")]
        [SerializeField] private Sprite redRocketSprite;
        [SerializeField] private Sprite blueRocketSprite;
        [SerializeField] private Sprite greenRocketSprite;
        [SerializeField] private Sprite yellowRocketSprite;

        [Header("Rocket Sprites")]
        [SerializeField] private Sprite horizontalRocketSprite;
        [SerializeField] private Sprite verticalRocketSprite;

        [Header("Rocket Part Sprites")]
        [SerializeField] private Sprite horizontalPartLeftSprite;
        [SerializeField] private Sprite horizontalPartRightSprite;
        [SerializeField] private Sprite verticalPartTopSprite;
        [SerializeField] private Sprite verticalPartBottomSprite;

        [Header("Obstacle Sprites")]
        [SerializeField] private Sprite boxSprite;
        [SerializeField] private Sprite stoneSprite;
        [SerializeField] private Sprite vaseFullSprite;
        [SerializeField] private Sprite vaseDamagedSprite;

        private GameObjectPool cubePool;

        private void Awake()
        {
            cubePool = new GameObjectPool(
                () => Instantiate(cubePrefab),
                transform, "CubePool", 10);
        }

        public Cube CreateCube(CubeColor color)
        {
            GameObject go = cubePool.Get();
            Cube cube = go.GetComponent<Cube>();
            cube.ResetForPool();
            cube.Initialize(color, GetDefaultSprite(color), GetRocketHintSprite(color));
            cube.SetPoolReturnAction(ReturnCube);
            go.name = $"Cube_{color}";
            return cube;
        }

        private void ReturnCube(GameObject go)
        {
            if (go == null || cubePool == null) return;
            Cube cube = go.GetComponent<Cube>();
            if (cube != null) cube.ResetForPool();
            cubePool.Return(go);
        }

        public Cube CreateRandomCube()
        {
            CubeColor color = GetRandomColor();
            return CreateCube(color);
        }

        public Rocket CreateRocket(RocketDirection direction)
        {
            GameObject go = Instantiate(rocketPrefab);
            Rocket rocket = go.GetComponent<Rocket>();
            Sprite sprite = direction == RocketDirection.Horizontal
                ? horizontalRocketSprite
                : verticalRocketSprite;
            rocket.Initialize(direction, sprite);
            go.name = $"Rocket_{direction}";
            return rocket;
        }

        public Box CreateBox()
        {
            GameObject go = Instantiate(boxPrefab);
            Box box = go.GetComponent<Box>();
            box.Initialize(boxSprite);
            go.name = "Box";
            return box;
        }

        public Stone CreateStone()
        {
            GameObject go = Instantiate(stonePrefab);
            Stone stone = go.GetComponent<Stone>();
            stone.Initialize(stoneSprite);
            go.name = "Stone";
            return stone;
        }

        public Vase CreateVase()
        {
            GameObject go = Instantiate(vasePrefab);
            Vase vase = go.GetComponent<Vase>();
            vase.Initialize(vaseFullSprite, vaseDamagedSprite);
            go.name = "Vase";
            return vase;
        }

        public BoardItem CreateFromToken(string token)
        {
            switch (token)
            {
                case "r": return CreateCube(CubeColor.Red);
                case "g": return CreateCube(CubeColor.Green);
                case "b": return CreateCube(CubeColor.Blue);
                case "y": return CreateCube(CubeColor.Yellow);
                case "rand": return CreateRandomCube();
                case "hro": return CreateRocket(RocketDirection.Horizontal);
                case "vro": return CreateRocket(RocketDirection.Vertical);
                case "bo": return CreateBox();
                case "s": return CreateStone();
                case "v": return CreateVase();
                default:
                    Debug.LogWarning($"ItemFactory: Unknown token '{token}', creating random cube.");
                    return CreateRandomCube();
            }
        }

        public CubeColor GetRandomColor()
        {
            int index = Random.Range(0, 4);
            return (CubeColor)index;
        }

        private Sprite GetDefaultSprite(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.Red: return redSprite;
                case CubeColor.Green: return greenSprite;
                case CubeColor.Blue: return blueSprite;
                case CubeColor.Yellow: return yellowSprite;
                default: return redSprite;
            }
        }

        public (Sprite partA, Sprite partB) GetRocketPartSprites(RocketDirection direction)
        {
            if (direction == RocketDirection.Horizontal)
                return (horizontalPartLeftSprite, horizontalPartRightSprite);
            else
                return (verticalPartBottomSprite, verticalPartTopSprite);
        }

        private Sprite GetRocketHintSprite(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.Red: return redRocketSprite;
                case CubeColor.Green: return greenRocketSprite;
                case CubeColor.Blue: return blueRocketSprite;
                case CubeColor.Yellow: return yellowRocketSprite;
                default: return redRocketSprite;
            }
        }
    }
}
