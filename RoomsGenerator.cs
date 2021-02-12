﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Samana.Generators
{
    public class RoomsGenerator
    {

        private static Random _rand = new Random();

        private Dictionary<Vector2, RoomBlock> _map;


        #region PUBLIC
        public RoomsGenerator()
        {
            _map = new Dictionary<Vector2, RoomBlock>();
        }

        public void Clear()
        {
            _map.Clear();
        }

        public void AddRoom(int minBlocks = 1, int maxBlocks = 1, SideState sideState = SideState.Door)
        {
            if (minBlocks < 1 || maxBlocks < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(minBlocks)} or {nameof(maxBlocks)} must be greater than zero.");
            }

            if (minBlocks > maxBlocks)
            {
                throw new ArgumentOutOfRangeException($"{nameof(minBlocks)} cannot be greater than {nameof(maxBlocks)}.");
            }


            int targetBlocksCount = _rand.Next(minBlocks, maxBlocks + 1);

            Room room = new Room();

            // первая комната без дверей создаётся с нулевых координат
            if (_map.Count == 0)
            {
                tryFillRoom(room, targetBlocksCount, 0, 0);
                return;
            }

            var allFreeSides = getFreeSidesOnMap().OrderBy(w => _rand.Next()).ToArray();

            // выстраиваем комнату возле свободной стены, если комната не помещается, то пробуем с другой стены пока не получится
            for (int i = 0; i < allFreeSides.Length; i++)
            {
                Side connectedSide = allFreeSides[i];
                Vector2 firstPosition = connectedSide.ToPosition();

                bool succes = tryFillRoom(room, targetBlocksCount, firstPosition.X, firstPosition.Y);
                if (!succes) continue;

                // если дошли сюда, значит комната вместилась и нужно сделать двери в первом блоке и начальной стене
                connectedSide.State = sideState;
                room.Blocks[0].GetSideByDirection(connectedSide.Direction.GetOpposite()).State = sideState;
                return;
            }
        }

        public void AddRooms(int roomsAmount = 1, int minBlocks = 1, int maxBlocks = 1, SideState sideState = SideState.Door)
        {
            if (roomsAmount < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(roomsAmount)} must be greater than zero.");
            }


            for (int i = 0; i < roomsAmount; i++)
            {
                AddRoom(minBlocks, maxBlocks, sideState);
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

        #endregion


        private bool tryFillRoom(Room room, int targetBlocksCount, int firstBlockX, int firstBlockY)
        {
            // добавили первый блок
            createAndAddBlockToRoomAndMap(firstBlockX, firstBlockY, room);

            // добавляем остальные блоки в комнату
            for (int i = 1; i < targetBlocksCount; i++)
            {
                // берём все сводобные стены у комнаты
                Side[] freeSidesOnRoom = getFreeSidesOnRoom(room);

                // если свободных стен нет, значит комнату невозможно достроить и нужно отчистить её и начать с другого места
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

                mergeSides(newBlock, room);
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

        private RoomBlock createAndAddBlockToRoomAndMap(int x, int y, Room room)
        {
            RoomBlock newBlock = new RoomBlock(x, y, room);
            room.Blocks.Add(newBlock);
            _map.Add(newBlock.Position, newBlock);

            return newBlock;
        }

        private void mergeSides(RoomBlock newBlock, Room room)
        {
            for (int i = 0; i < newBlock.Sides.Length; i++)
            {
                var nearBlock = room.Blocks.FirstOrDefault(b => b.Position == newBlock.Sides[i].ToPosition());
                if (nearBlock != null)
                {
                    newBlock.Sides[i].State = SideState.None;
                    Vector2 oppositeDirection = newBlock.Sides[i].Direction.GetOpposite();
                    nearBlock.GetSideByDirection(oppositeDirection).State = SideState.None;
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


        private class Room
        {
            public int GetDoorsCount
            {
                get
                {
                    if (Blocks == null) return 0;
                    return Blocks.SelectMany(b => b.Sides).Where(w => w.State == SideState.Door).Count();
                }
            }

            public List<RoomBlock> Blocks = new List<RoomBlock>();
        }


        private class RoomBlock
        {
            public Vector2 Position;
            public Room ParentRoom;
            public Side[] Sides;

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

        private class Side
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

        private struct Vector2
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
    }

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

}
