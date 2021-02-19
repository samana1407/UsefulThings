using System;
using System.Collections.Generic;
using System.Linq;

namespace Samana.Generators
{
    public class RoomsGenerator
    {

        private static Random _rand = new Random();

        private Dictionary<Vector2, RoomBlock> _map;


        #region PUBLIC METHODS
        public RoomsGenerator()
        {
            _map = new Dictionary<Vector2, RoomBlock>();
        }

        public void Clear()
        {
            _map.Clear();
        }

        public void AddSolidRoom(int minBlocksCount = 1, int maxBlocksCount = 1)
        {
            Room newRoom = addBaseRoom(minBlocksCount, maxBlocksCount);
            setInnerSidesTo(newRoom, SideState.None);
        }

        public void AddSolidRooms(int roomsCount = 1, int minBlocksCount = 1, int maxBlocksCount = 1)
        {
            if (roomsCount < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(roomsCount)} must be greater than zero.");
            }


            for (int i = 0; i < roomsCount; i++)
            {
                AddSolidRoom(minBlocksCount, maxBlocksCount);
            }
        }

        public void AddRoomWithPartitions(int minBlocksCount = 1, int maxBlocksCount = 1)
        {
            Room newRoom = addBaseRoom(minBlocksCount, maxBlocksCount);
            setInnerSidesTo(newRoom, SideState.Wall);
            constructPartitionWalls(newRoom);
        }

        public void AddRoomsWithPartitions(int roomsCount = 1, int minBlocksCount = 1, int maxBlocksCount = 1)
        {
            if (roomsCount < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(roomsCount)} must be greater than zero.");
            }

            for (int i = 0; i < roomsCount; i++)
            {
                Room newRoom = addBaseRoom(minBlocksCount, maxBlocksCount);
                setInnerSidesTo(newRoom, SideState.Wall);
                constructPartitionWalls(newRoom);
            }
        }

        public List<RoomData> GetRoomsData()
        {
            clampPositions();
            List<Room> rooms = _map.Values.Select(rb => rb.ParentRoom).Distinct().ToList();


            List<RoomData> outRoomsData = new List<RoomData>();

            foreach (Room room in rooms)
            {
                RoomData roomData = new RoomData();
                foreach (RoomBlock block in room.Blocks)
                {
                    BlockData blockData = new BlockData();
                    blockData.X = block.Position.X;
                    blockData.Y = block.Position.Y;
                    blockData.UpSide = block.GetSideByDirection(Vector2.UP).State;
                    blockData.RightSide = block.GetSideByDirection(Vector2.RIGHT).State;
                    blockData.DownSide = block.GetSideByDirection(Vector2.DOWN).State;
                    blockData.LeftSide = block.GetSideByDirection(Vector2.LEFT).State;

                    roomData.Blocks.Add(blockData);
                }

                outRoomsData.Add(roomData);
            }

            return outRoomsData;
        }

        public List<Room> GetRawRoomsData()
        {
            clampPositions();
            return _map.Values.Select(b => b.ParentRoom).Distinct().ToList();
        }

        #endregion

        #region PRIVATE METHODS
        private RoomBlock createAndAddBlockToRoomAndMap(int x, int y, Room room)
        {
            RoomBlock newBlock = new RoomBlock(x, y, room);
            room.Blocks.Add(newBlock);
            _map.Add(newBlock.Position, newBlock);

            return newBlock;
        }

        // расставляет внутри комнаты случайные перегородки
        private void constructPartitionWalls(Room room)
        {
            setInnerSidesTo(room, SideState.Wall);

            var shuffledBlocks = room.Blocks.OrderBy(_ => _rand.Next()).ToList();

            var addedBlocks = new List<RoomBlock>();
            addedBlocks.Add(shuffledBlocks[0]);


            while (addedBlocks.Count != shuffledBlocks.Count)
            {
                var block = addedBlocks[_rand.Next(addedBlocks.Count)];

                // найти все  соседние блоки этой же комнаты, исключая уже добавленные блоки
                var shuffledSides = block.Sides.OrderBy(_ => _rand.Next()).ToArray();

                for (int j = 0; j < shuffledSides.Length; j++)
                {
                    Side currentSide = shuffledSides[j];
                    Vector2 neighbourPos = currentSide.ToPosition();
                    RoomBlock neighbourBlock = room.Blocks.FirstOrDefault(b => b.Position == neighbourPos);
                    if (neighbourBlock != null && !addedBlocks.Contains(neighbourBlock))
                    {
                        currentSide.State = SideState.None;
                        neighbourBlock.GetSideByDirection(currentSide.Direction.GetOpposite()).State = SideState.None;
                        addedBlocks.Add(neighbourBlock);
                        break;
                    }
                }
            }
        }


        private Side[] getFreeSidesOnMap()
        {
            var allFreeSides = _map.Values.SelectMany(rb => rb.Sides)
                                        .Where(side => !_map.ContainsKey(side.ToPosition())).ToArray();
            return allFreeSides;
        }
        private Side[] getFreeSidesOnRoom(Room room)
        {
            var allFreeSides = room.Blocks.SelectMany(rb => rb.Sides)
                                        .Where(side => !_map.ContainsKey(side.ToPosition())).ToArray();
            return allFreeSides;
        }


        private Side[] getOuterSidesOnRoom(Room room)
        {
            var roomBlocksPositions = room.Blocks.Select(b => b.Position).ToList();

            return room.Blocks.SelectMany(rb => rb.Sides)
                            .Where(s => !roomBlocksPositions.Contains(s.ToPosition())).ToArray();
        }
        private Side[] getInnerSidesOnRoom(Room room)
        {
            var allSides = room.Blocks.SelectMany(b => b.Sides).ToArray();

            return allSides.Except(getOuterSidesOnRoom(room)).ToArray();
        }
        private void setInnerSidesTo(Room room, SideState targetState = SideState.None)
        {

            foreach (Side side in getInnerSidesOnRoom(room))
            {
                side.State = targetState;
            }
        }



        // создаёт базовую комнату из блоков. 
        // все стороны блоков в состоянии по-умолчанию (стена)
        // базовая комната автоматически соединяется дверью
        private Room addBaseRoom(int minBlocksCount = 1, int maxBlocksCount = 1)
        {
            if (minBlocksCount < 1 || maxBlocksCount < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(minBlocksCount)} or {nameof(maxBlocksCount)} must be greater than zero.");
            }

            if (minBlocksCount > maxBlocksCount)
            {
                throw new ArgumentOutOfRangeException($"{nameof(minBlocksCount)} cannot be greater than {nameof(maxBlocksCount)}.");
            }


            int targetBlocksCount = _rand.Next(minBlocksCount, maxBlocksCount + 1);

            Room room = new Room();

            // первая комната без дверей создаётся с нулевых координат
            if (_map.Count == 0)
            {
                tryConstructRoom(room, targetBlocksCount, 0, 0);
                return room;
            }

            var allFreeSides = getFreeSidesOnMap().OrderBy(w => _rand.Next()).ToArray();

            // выстраиваем комнату возле свободной стены, если комната не помещается, то пробуем с другой стены пока не получится
            for (int i = 0; i < allFreeSides.Length; i++)
            {
                Side connectedSide = allFreeSides[i];
                Vector2 firstPosition = connectedSide.ToPosition();

                // если комнату неудалось полностью уместить в данном месте, то пробуем с другого места. 
                bool succes = tryConstructRoom(room, targetBlocksCount, firstPosition.X, firstPosition.Y);
                if (!succes) continue;

                // если дошли сюда, значит комната вместилась и нужно сделать двери в первом блоке и начальной стене
                connectedSide.State = SideState.Door;
                room.Blocks[0].GetSideByDirection(connectedSide.Direction.GetOpposite()).State = SideState.Door;

                // установить соседство соединённым комнатам, если их соединение не является стеной
                defineDoorHeighbourhood(room, connectedSide.ParentRoomBlock.ParentRoom);

                break;
            }

            return room;
        }
        private bool tryConstructRoom(Room room, int targetBlocksCount, int firstBlockX, int firstBlockY)
        {
            // добавили первый блок
            createAndAddBlockToRoomAndMap(firstBlockX, firstBlockY, room);

            // добавляем остальные блоки в комнату
            for (int i = 1; i < targetBlocksCount; i++)
            {
                // берём все сводобные стены у комнаты
                Side[] freeSidesOnRoom = getFreeSidesOnRoom(room);

                // если свободных стен нет, значит комнату невозможно достроить
                // и нужно отчистить её и вернуть результат о неудаче
                if (freeSidesOnRoom.Length == 0)
                {
                    //Console.WriteLine("no free side on room");
                    //отчистить комнату и начать с другой стены на карте
                    foreach (RoomBlock roomBlock in room.Blocks)
                    {
                        _map.Remove(roomBlock.Position);
                    }
                    room.Blocks.Clear();

                    return false;
                }

                // создание нового блока комнаты возле случайной свободной стены у комнаты
                Side randSide = freeSidesOnRoom[_rand.Next(freeSidesOnRoom.Length)];
                Vector2 blockPos = randSide.ToPosition();
                RoomBlock newBlock = createAndAddBlockToRoomAndMap(blockPos.X, blockPos.Y, room);
            }

            return true;
        }
        private void clampPositions()
        {
            if (_map.Count == 0) return;

            int minX = _map.Keys.Min(p => p.X);
            int minY = _map.Keys.Min(p => p.Y);

            foreach (var rb in _map.Values)
            {
                rb.Position = new Vector2(rb.Position.X - minX, rb.Position.Y - minY);
            }

            var tempMap = new Dictionary<Vector2, RoomBlock>();
            _map = _map.Values.ToDictionary(b => b.Position, b => b);
        }

        // установить соседство (сосед это та комната с которой есть соединение дверью)
        private void defineDoorHeighbourhood(Room roomA, Room roomB)
        {
            if (!roomA.DoorNeighbours.Contains(roomB)) roomA.DoorNeighbours.Add(roomB);
            if (!roomB.DoorNeighbours.Contains(roomA)) roomB.DoorNeighbours.Add(roomA);
        }
        #endregion

        #region PRIVATE CLASSES

        public class Room
        {
            public List<Room> DoorNeighbours;
            public List<RoomBlock> Blocks;
            public int GetDoorsCount
            {
                get
                {
                    if (Blocks == null) return 0;
                    return Blocks.SelectMany(b => b.Sides).Where(w => w.State == SideState.Door).Count();
                }
            }
            public Room()
            {
                Blocks = new List<RoomBlock>();
                DoorNeighbours = new List<Room>();
            }
        }


        public class RoomBlock
        {
            public Vector2 Position;
            public Room ParentRoom;
            public readonly Side[] Sides;

            //public Side UpSide => get Sides[0];
            public Side UpSide { get => Sides[0]; }
            public Side RightSide { get => Sides[1]; }
            public Side DownSide { get => Sides[2]; }
            public Side LeftSide { get => Sides[3]; }

            public int X { get => Position.X; set => Position.X = value; }
            public int Y { get => Position.Y; set => Position.Y = value; }

            public RoomBlock(int x, int y, Room parentRoom)
            {
                Position = new Vector2(x, y);
                ParentRoom = parentRoom;
                Sides = new Side[4]
                {
                new Side(this,Vector2.UP),// top
                new Side(this,Vector2.RIGHT),// right
                new Side(this,Vector2.DOWN),// down
                new Side(this,Vector2.LEFT),// left
                };
            }

            public Side GetSideByDirection(Vector2 direction)
            {
                foreach (Side side in Sides)
                {
                    if (side.Direction == direction) return side;
                }

                return null;
            }

        }

        public class Side
        {
            public RoomBlock ParentRoomBlock;
            public SideState State;
            public readonly Vector2 Direction;

            public Side(RoomBlock parentRoomBlock, Vector2 direction, SideState state = SideState.Wall)
            {
                ParentRoomBlock = parentRoomBlock;
                Direction = direction;
                State = state;
            }

            // на какую клетку указывает стена
            public Vector2 ToPosition() => ParentRoomBlock.Position + Direction;
        }

        public struct Vector2
        {
            public readonly static Vector2 UP = new Vector2(0, -1);
            public readonly static Vector2 DOWN = new Vector2(0, 1);
            public readonly static Vector2 RIGHT = new Vector2(1, 0);
            public readonly static Vector2 LEFT = new Vector2(-1, 0);

            public int X, Y;

            public Vector2(int x, int y)
            {
                X = x;
                Y = y;
            }
            public Vector2 GetOpposite() => new Vector2(X * -1, Y * -1);

            public static Vector2 operator +(Vector2 a, Vector2 b)
            {
                return new Vector2(a.X + b.X, a.Y + b.Y);
            }
            public static bool operator ==(Vector2 a, Vector2 b)
            {
                return (a.X == b.X) && (a.Y == b.Y);
            }
            public static bool operator !=(Vector2 a, Vector2 b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is Vector2)
                    return this == (Vector2)obj;

                else return false;
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }

        #endregion

    }


    #region PUBLIC OUT CLASSES
    public enum SideState : int
    {
        None = 0,
        Wall = 1,
        Door = 2,
    }

    public class RoomData
    {
        public List<BlockData> Blocks;

        public RoomData()
        {
            Blocks = new List<BlockData>();
        }
    }

    public class BlockData
    {
        public int X;
        public int Y;
        public SideState UpSide;
        public SideState RightSide;
        public SideState DownSide;
        public SideState LeftSide;
    }
    #endregion
}
