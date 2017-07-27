using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TicTacToe
{
    public struct Option<T>
    {
        public Option(T value)
        {
            HasValue = true;
            Value = value;
        }

        public U Fold<U>(Func<U> none, Func<T, U> some)
        {
            return HasValue ? some(Value) : none();
        }

        public override string ToString()
        {
            return Fold(() => "None", x => string.Format("Some{{{0}}}", x));
        }

        private readonly bool HasValue;
        private readonly T Value;
    }

    public static class Option
    {
        public static Option<T> Some<T>(T value)
        {
            return new Option<T>(value);
        }
        public static Option<T> None<T>()
        {
            return new Option<T>();
        }
        public static Option<TResult> Select<TSource, TResult>(this Option<TSource> source, Func<TSource, TResult> selector)
        {
            return source.Fold(None<TResult>, value => Some(selector(value)));
        }
        public static Option<TResult> SelectMany<TSource, TResult>(this Option<TSource> source, Func<TSource, Option<TResult>> selector)
        {
            return source.Fold(None<TResult>, value => selector(value));
        }
        public static Option<TResult> SelectMany<TSource, TCollection, TResult>(this Option<TSource> source, Func<TSource, Option<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            return source.Fold(None<TResult>, value => collectionSelector(value).Fold(None<TResult>, collection => Option.Some(resultSelector(value, collection))));
        }
        public static TSource FirstOrDefault<TSource>(this Option<TSource> source)
        {
            return source.Fold(() => default(TSource), value => value);
        }
        public static bool Any<TSource>(this Option<TSource> source)
        {
            return source.Fold(() => false, _ => true);
        }
    }

    public enum Player { Player0, Player1 }

    public static class PlayerExtensions
    {
        public static T Fold<T>(this Player player, T player0, T player1) { return player == Player.Player0 ? player0 : player1; }
        public static Player alternate(this Player player) { return player.Fold(Player.Player1, Player.Player0); }
        public static char ToChar(this Player player) { return player.Fold('X', 'O'); }
        public static string toString(this Player player) { return player.Fold("Player 0", "Player 1"); }
    }

    public enum Position { NW, N, NE, W, C, E, SW, S, SE }

    public static class PositionExtensions
    {
        public static char ToChar(this Position position) { return (char)('0' + (int)position); }
        public static Option<Position> ToPosition(this char c) { return c >= '0' && c <= '8' ? Option.Some((Position)(c - '0')) : Option.None<Position>(); }
    }

    public struct _0 { }
    public struct _1 { }
    public struct _2 { }
    public struct _3 { }
    public struct _4 { }
    public struct _5 { }
    public struct _6 { }
    public struct _7 { }
    public struct _8 { }
    public struct Draw { }
    public struct _5Win { }
    public struct _6Win { }
    public struct _7Win { }
    public struct _8Win { }
    public struct _9Win { }

    public struct Section
    {
        public readonly ulong Mask;
        public readonly int Offset;

        public Section(int bits, int Offset)
        {
            this.Mask = (1ul << bits) - 1;
            this.Offset = Offset;
        }
    }

    public struct Game<T> : IEquatable<Game<T>>
    {
        public readonly ulong data;
        public bool Equals(Game<T> other) { return data == other.data; }
        public override bool Equals(object obj) { return obj is Game<T> && Equals((Game<T>)obj); }
        public override int GetHashCode() { return data.GetHashCode(); }
        public static bool operator ==(Game<T> x, Game<T> y) { return x.Equals(y); }
        public static bool operator !=(Game<T> x, Game<T> y) { return !x.Equals(y); }
        internal Game(ulong data) { this.data = data; }
    }

    public static class Game
    {
        public static Game<_0> Game0 = new Game<_0>(0);
    }


    public struct MoveResult<T, U>
    {
        public MoveResult(Game<T> t) : this(0, t.data) { }

        public MoveResult(Game<U> u) : this(1, u.data) { }

        public V Fold<V>(Func<Game<T>, V> left, Func<Game<U>, V> right) { return value == 0 ? left(new Game<T>(data)) : right(new Game<U>(data)); }

        public override string ToString() { return Fold(x => x.ToString(), x => x.ToString()); }

        private MoveResult(sbyte value, ulong data)
        {
            this.value = value;
            this.data = data;
        }

        private readonly sbyte value;
        private readonly ulong data;
    }

    public static class Extensions
    {
        public static IEnumerable<T> Unfold<State, T>(this State state, Func<State, Option<KeyValuePair<T, State>>> generator)
        {
            for (var pair = new KeyValuePair<T, State>(default(T), state); generator(pair.Value).Fold(() => false, some => { pair = some; return true; }); )
                yield return pair.Key;
        }

        public static Option<Game<U>> GetWin<T, U>(this MoveResult<T, U> result) { return result.Fold(_ => Option.None<Game<U>>(), win => Option.Some(win)); }

        public static Option<Game<T>> GetGame<T, U>(this MoveResult<T, U> result) { return result.Fold(game => Option.Some(game), _ => Option.None<Game<T>>()); }

        public static ulong get(this ulong data, Section section) { return (data & section.Mask << section.Offset) >> section.Offset; }

        public static ulong set(this ulong data, Section section, ulong value)
        {
            var offsetMask = section.Mask << section.Offset;
            return (data & ~offsetMask) | (value << section.Offset & offsetMask);
        }

        private const int numberOfPositions = 9;
        private const int bitsPerPositionListEntry = 4;
        private const int positionListLength = numberOfPositions * bitsPerPositionListEntry;
        private const int bitsPerPlayerMapEntry = 2;
        private const int positionListOffset = 0;
        private static readonly Section positionListSection = new Section(positionListLength, positionListOffset);
        private static readonly Section firstEntryInPositionList = new Section(bitsPerPositionListEntry, positionListOffset);
        private static readonly Section whoseTurnSection = new Section(1, 63);

        private static Section positionSection(int position) { return new Section(bitsPerPlayerMapEntry, positionListLength + position * bitsPerPlayerMapEntry); }

        public static Option<Player> playerAt(this ulong moves, Position position)
        {
            switch (moves.get(positionSection((int)position)))
            {
                case 0: return Option.None<Player>();
                case 1: return Option.Some(Player.Player0);
                default: return Option.Some(Player.Player1);
            }
        }

        public static Player whoseTurn(this ulong moves) { return moves.get(whoseTurnSection) == 0 ? Player.Player0 : Player.Player1; }

        private static ulong move(this ulong moves, ulong position, ulong player, ulong positionList)
        {
            return moves
                .set(positionSection((int)position), player)
                .set(positionListSection, positionList)
                .set(whoseTurnSection, ~moves.get(whoseTurnSection));
        }

        public static ulong moveForward(this ulong moves, Position position)
        {
            return moves.move((ulong)position, moves.whoseTurn().Fold(1ul, 2ul), moves.get(positionListSection) << bitsPerPositionListEntry | (uint)position);
        }

        public static ulong moveBackward(this ulong moves)
        {
            return moves.move(moves.get(firstEntryInPositionList), 0, moves.get(positionListSection) >> bitsPerPositionListEntry);
        }

        public static Option<Player> playerAt<T>(this Game<T> game, Position position) { return game.data.playerAt(position); }

        public static string ToString(this ulong moves, Func<Position, char> ToChar)
        {
            var toChar = new Func<Position, Char>(position => moves.playerAt(position).Fold(() => ToChar(position), value => value.ToChar()));
            return string.Format(".===.===.===.\n| {0} | {1} | {2} |\n.===.===.===.\n| {3} | {4} | {5} |\n.===.===.===.\n| {6} | {7} | {8} |\n.===.===.===.",
                toChar(Position.NW), toChar(Position.N), toChar(Position.NE), toChar(Position.W), toChar(Position.C), toChar(Position.E), toChar(Position.SW), toChar(Position.S), toChar(Position.SE));
        }

        private static Option<U> moveThatMightFail<T, U>(this Game<T> game, Position position, Func<ulong, U> selector)
        {
            return game.data.playerAt(position).Fold(() => Option.Some(selector(game.data.moveForward(position))), _ => Option.None<U>());
        }

        private static Game<U> moveBackward<T, U>(this Game<T> game) { return new Game<U>(game.data.moveBackward()); }

        private static Option<Game<T>> simpleMove<T, U>(this Game<U> game, Position position) { return game.moveThatMightFail(position, x => new Game<T>(x)); }

        private static IEnumerable<IEnumerable<Position>> wins = new[] {
            new[] { Position.NW, Position.W, Position.SW},
            new[] { Position.N,  Position.C, Position.S },
            new[] { Position.NE, Position.E, Position.SE},
            new[] { Position.NW, Position.N, Position.NE},
            new[] { Position.W,  Position.C, Position.E},
            new[] { Position.SW, Position.S, Position.SE},
            new[] { Position.NW, Position.C, Position.SE},
            new[] { Position.NE, Position.C, Position.SW},
        };

        private static MoveResult<T, U> partition<T, U>(this ulong moves)
        {
            var player = moves.whoseTurn().alternate();
            return wins.Any(win => win.All(position => moves.playerAt(position).Fold(() => false, value => value == player))) ? new MoveResult<T, U>(new Game<U>(moves)) : new MoveResult<T, U>(new Game<T>(moves));
        }

        private static Option<MoveResult<T, U>> complexMove<T, U, V>(this Game<V> game, Position position) { return game.moveThatMightFail(position, partition<T, U>); }

        public static Game<_1> move(this Game<_0> game, Position position) { return new Game<_1>(game.data.moveForward(position)); }
        public static Option<Game<_2>> move(this Game<_1> game, Position position) { return game.simpleMove<_2, _1>(position); }
        public static Option<Game<_3>> move(this Game<_2> game, Position position) { return game.simpleMove<_3, _2>(position); }
        public static Option<Game<_4>> move(this Game<_3> game, Position position) { return game.simpleMove<_4, _3>(position); }
        public static Option<MoveResult<_5, _5Win>> move(this Game<_4> game, Position position) { return game.complexMove<_5, _5Win, _4>(position); }
        public static Option<MoveResult<_6, _6Win>> move(this Game<_5> game, Position position) { return game.complexMove<_6, _6Win, _5>(position); }
        public static Option<MoveResult<_7, _7Win>> move(this Game<_6> game, Position position) { return game.complexMove<_7, _7Win, _6>(position); }
        public static Option<MoveResult<_8, _8Win>> move(this Game<_7> game, Position position) { return game.complexMove<_8, _8Win, _7>(position); }
        public static Option<MoveResult<Draw, _9Win>> move(this Game<_8> game, Position position) { return game.complexMove<Draw, _9Win, _8>(position); }
        public static Game<_0> takeBack(this Game<_1> game) { return game.moveBackward<_1, _0>(); }
        public static Game<_1> takeBack(this Game<_2> game) { return game.moveBackward<_2, _1>(); }
        public static Game<_2> takeBack(this Game<_3> game) { return game.moveBackward<_3, _2>(); }
        public static Game<_3> takeBack(this Game<_4> game) { return game.moveBackward<_4, _3>(); }
        public static Game<_4> takeBack(this Game<_5> game) { return game.moveBackward<_5, _4>(); }
        public static Game<_5> takeBack(this Game<_6> game) { return game.moveBackward<_6, _5>(); }
        public static Game<_6> takeBack(this Game<_7> game) { return game.moveBackward<_7, _6>(); }
        public static Game<_7> takeBack(this Game<_8> game) { return game.moveBackward<_8, _7>(); }
        public static Game<_8> takeBack(this Game<Draw> game) { return game.moveBackward<Draw, _8>(); }
        public static Game<_4> takeBack(this Game<_5Win> game) { return game.moveBackward<_5Win, _4>(); }
        public static Game<_5> takeBack(this Game<_6Win> game) { return game.moveBackward<_6Win, _5>(); }
        public static Game<_6> takeBack(this Game<_7Win> game) { return game.moveBackward<_7Win, _6>(); }
        public static Game<_7> takeBack(this Game<_8Win> game) { return game.moveBackward<_8Win, _7>(); }
        public static Game<_8> takeBack(this Game<_9Win> game) { return game.moveBackward<_9Win, _8>(); }
    }

    public struct UserInterfaceState
    {
        public UserInterfaceState(Game<_0> game, TextWriter writer, TextReader reader) : this(0, game.data, writer, reader) { }
        public UserInterfaceState(Game<_1> game, TextWriter writer, TextReader reader) : this(1, game.data, writer, reader) { }
        public UserInterfaceState(Game<_2> game, TextWriter writer, TextReader reader) : this(2, game.data, writer, reader) { }
        public UserInterfaceState(Game<_3> game, TextWriter writer, TextReader reader) : this(3, game.data, writer, reader) { }
        public UserInterfaceState(Game<_4> game, TextWriter writer, TextReader reader) : this(4, game.data, writer, reader) { }
        public UserInterfaceState(Game<_5> game, TextWriter writer, TextReader reader) : this(5, game.data, writer, reader) { }
        public UserInterfaceState(Game<_6> game, TextWriter writer, TextReader reader) : this(6, game.data, writer, reader) { }
        public UserInterfaceState(Game<_7> game, TextWriter writer, TextReader reader) : this(7, game.data, writer, reader) { }
        public UserInterfaceState(Game<_8> game, TextWriter writer, TextReader reader) : this(8, game.data, writer, reader) { }
        public UserInterfaceState(Game<Draw> game, TextWriter writer, TextReader reader) : this(9, game.data, writer, reader) { }
        public UserInterfaceState(Game<_5Win> game, TextWriter writer, TextReader reader) : this(10, game.data, writer, reader) { }
        public UserInterfaceState(Game<_6Win> game, TextWriter writer, TextReader reader) : this(11, game.data, writer, reader) { }
        public UserInterfaceState(Game<_7Win> game, TextWriter writer, TextReader reader) : this(12, game.data, writer, reader) { }
        public UserInterfaceState(Game<_8Win> game, TextWriter writer, TextReader reader) : this(13, game.data, writer, reader) { }
        public UserInterfaceState(Game<_9Win> game, TextWriter writer, TextReader reader) : this(14, game.data, writer, reader) { }
        public UserInterfaceState(UserInterfaceState state, Func<Position, char> toChar) : this(state.value, state.data, state.writer, state.reader, toChar) { }

        public V Fold<V>(Func<Game<_0>, V> game0, Func<Game<_1>, V> game1, Func<Game<_2>, V> game2, Func<Game<_3>, V> game3, Func<Game<_4>, V> game4, Func<Game<_5>, V> game5, Func<Game<_6>, V> game6, Func<Game<_7>, V> game7, Func<Game<_8>, V> game8, Func<Game<Draw>, V> draw, Func<Game<_5Win>, V> win5, Func<Game<_6Win>, V> win6, Func<Game<_7Win>, V> win7, Func<Game<_8Win>, V> win8, Func<Game<_9Win>, V> win9)
        {
            switch(value)
            {
                case 0: return game0(new Game<_0>(data));
                case 1: return game1(new Game<_1>(data));
                case 2: return game2(new Game<_2>(data));
                case 3: return game3(new Game<_3>(data));
                case 4: return game4(new Game<_4>(data));
                case 5: return game5(new Game<_5>(data));
                case 6: return game6(new Game<_6>(data));
                case 7: return game7(new Game<_7>(data));
                case 8: return game8(new Game<_8>(data));
                case 9: return draw(new Game<Draw>(data));
                case 10: return win5(new Game<_5Win>(data));
                case 11: return win6(new Game<_6Win>(data));
                case 12: return win7(new Game<_7Win>(data));
                case 13: return win8(new Game<_8Win>(data));
                default: return win9(new Game<_9Win>(data));
            }
        }

        public static char space<T>(T _) { return ' '; }

        private UserInterfaceState(sbyte value, ulong data, TextWriter writer, TextReader reader, Func<Position, Char> toChar = null)
        {
            this.value = value;
            this.data = data;
            this.writer = writer;
            this.reader = reader;
            this.toChar = toChar ?? space;
        }

        public readonly sbyte value;
        public readonly ulong data;
        public readonly TextWriter writer;
        public readonly TextReader reader;
        public Func<Position, char> toChar;
    }

    public struct MoverOrWinner
    {
        public MoverOrWinner(Func<Position, Option<UserInterfaceState>> mover) : this(0, mover, Option.None<Player>()) { }
        public MoverOrWinner(Option<Player> winner) : this(1, null, winner) { }
        public T Fold<T>(Func<Func<Position, Option<UserInterfaceState>>, T> mover, Func<Option<Player>, T> winner) { return value == 0 ? mover(this.mover) : winner(this.winner); }

        private readonly sbyte value;
        private readonly Func<Position, Option<UserInterfaceState>> mover;
        private readonly Option<Player> winner;

        private MoverOrWinner(sbyte value, Func<Position, Option<UserInterfaceState>> mover, Option<Player> winner)
        {
            this.value = value;
            this.mover = mover;
            this.winner = winner;
        }
    }

    public static class StateExtensions
    {
        public static MoverOrWinner getMoverOrWinner(this UserInterfaceState state)
        {
            return state.Fold(
                game0 => new MoverOrWinner(position => Option.Some(new UserInterfaceState(game0.move(position), state.writer, state.reader))),
                game1 => new MoverOrWinner(position => from _2 in game1.move(position) select new UserInterfaceState(_2, state.writer, state.reader)),
                game2 => new MoverOrWinner(position => from _3 in game2.move(position) select new UserInterfaceState(_3, state.writer, state.reader)),
                game3 => new MoverOrWinner(position => from _4 in game3.move(position) select new UserInterfaceState(_4, state.writer, state.reader)),
                game4 => new MoverOrWinner(position => from _5 in game4.move(position) select _5.Fold(game5 => new UserInterfaceState(game5, state.writer, state.reader), win5 => new UserInterfaceState(win5, state.writer, state.reader))),
                game5 => new MoverOrWinner(position => game5.move(position).Select(result => result.Fold(game6 => new UserInterfaceState(game6, state.writer, state.reader), win6 => new UserInterfaceState(win6, state.writer, state.reader)))),
                game6 => new MoverOrWinner(position => game6.move(position).Select(result => result.Fold(game7 => new UserInterfaceState(game7, state.writer, state.reader), win7 => new UserInterfaceState(win7, state.writer, state.reader)))),
                game7 => new MoverOrWinner(position => game7.move(position).Select(result => result.Fold(game8 => new UserInterfaceState(game8, state.writer, state.reader), win8 => new UserInterfaceState(win8, state.writer, state.reader)))),
                game8 => new MoverOrWinner(position => game8.move(position).Select(result => result.Fold(game9 => new UserInterfaceState(game9, state.writer, state.reader), win9 => new UserInterfaceState(win9, state.writer, state.reader)))),
                game9 => new MoverOrWinner(Option.None<Player>()),
                win5 => new MoverOrWinner(Option.Some(Player.Player0)),
                win6 => new MoverOrWinner(Option.Some(Player.Player1)),
                win7 => new MoverOrWinner(Option.Some(Player.Player0)),
                win8 => new MoverOrWinner(Option.Some(Player.Player1)),
                win9 => new MoverOrWinner(Option.Some(Player.Player0))
                );
        }

        public static Option<UserInterfaceState> getTakeBack(this UserInterfaceState state)
        {
            return state.Fold(game => Option.None<UserInterfaceState>(),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader)),
                game => Option.Some(new UserInterfaceState(game.takeBack(), state.writer, state.reader))
                );
        }
    }

    class Program
    {
        private static Func<UserInterfaceState> invalidSelection(TextWriter writer, UserInterfaceState state)
        {
            return () =>
            {
                writer.WriteLine("Invalid selection. Please try again.");
                return state;
            };
        }

        private static Func<Func<Position, Option<UserInterfaceState>>, TextWriter> printMoves(TextWriter writer, Player whoseTurn)
        {
            return _ =>
            {
                writer.WriteLine("{0} to move [{1}]", whoseTurn.toString(), whoseTurn.ToChar());
                writer.WriteLine("  [0-8] to Move");
                return writer;
            };
        }

        private static Func<Option<Player>, TextWriter> printWinner(TextWriter writer)
        {
            return winner =>
            {
                writer.WriteLine(winner.Fold(() => "Draw", player => string.Format("{0} Wins!", player.toString())));
                return writer;
            };
        }

        private static Option<KeyValuePair<UserInterfaceState, UserInterfaceState>> gameLoop(UserInterfaceState state)
        {
            state.writer.WriteLine("\n\n{0}\n", state.data.ToString(state.toChar));

            var moverOrWinner = state.getMoverOrWinner();
            moverOrWinner.Fold(printMoves(state.writer, state.data.whoseTurn()), printWinner(state.writer));

            state.writer.WriteLine("  q to Quit");
            state.writer.WriteLine("  v to view board positions");

            var takeBack = state.getTakeBack();
            if(takeBack.Any()) state.writer.WriteLine("  t to take back last move");

            state.writer.Write("  > ");

            var line = state.reader.ReadLine();
            if (line == string.Empty)
            {
                state.writer.WriteLine("Please make a selection.");
                return Option.Some(new KeyValuePair<UserInterfaceState, UserInterfaceState>(default(UserInterfaceState), state));
            }

            var c = char.ToUpper(line[0]);
            switch (c)
            {
                case 'Q':
                    state.writer.WriteLine("Bye!");
                    return Option.None<KeyValuePair<UserInterfaceState, UserInterfaceState>>();
                case 'V':
                    return Option.Some(new KeyValuePair<UserInterfaceState, UserInterfaceState>(default(UserInterfaceState), new UserInterfaceState(state, PositionExtensions.ToChar)));
                case 'T':
                    return Option.Some(new KeyValuePair<UserInterfaceState, UserInterfaceState>(default(UserInterfaceState), takeBack.Fold(invalidSelection(state.writer, state), s => s)));
                default:
                    return Option.Some(new KeyValuePair<UserInterfaceState, UserInterfaceState>(default(UserInterfaceState), (from mover in moverOrWinner.Fold(mover => Option.Some(mover), _ => Option.None<Func<Position, Option<UserInterfaceState>>>())
                                                                                                       from position in c.ToPosition()
                                                                                                       select mover(position)).FirstOrDefault().Fold(invalidSelection(state.writer, state), s => s)));
            }
        }

        public static Game<_1> build(Position p0) { return Game.Game0.move(p0); }
        public static Option<Game<_2>> build(Position p0, Position p1) { return build(p0).move(p1); }
        public static Option<Game<_3>> build(Position p0, Position p1, Position p2) { return build(p0, p1).SelectMany(_2 => _2.move(p2)); }
        public static Option<Game<_4>> build(Position p0, Position p1, Position p2, Position p3) { return build(p0, p1, p2).SelectMany(_3 => _3.move(p3)); }
        public static Option<MoveResult<_5, _5Win>> build(Position p0, Position p1, Position p2, Position p3, Position p4) { return build(p0, p1, p2, p3).SelectMany(_4 => _4.move(p4)); }
        public static Option<MoveResult<_6, _6Win>> build(Position p0, Position p1, Position p2, Position p3, Position p4, Position p5) { return build(p0, p1, p2, p3, p4).SelectMany(Extensions.GetGame).SelectMany(_5 => _5.move(p5)); }
        public static Option<MoveResult<_7, _7Win>> build(Position p0, Position p1, Position p2, Position p3, Position p4, Position p5, Position p6) { return build(p0, p1, p2, p3, p4, p5).SelectMany(Extensions.GetGame).SelectMany(_6 => _6.move(p6)); }
        public static Option<MoveResult<_8, _8Win>> build(Position p0, Position p1, Position p2, Position p3, Position p4, Position p5, Position p6, Position p7) { return build(p0, p1, p2, p3, p4, p5, p6).SelectMany(Extensions.GetGame).SelectMany(_7 => _7.move(p7)); }
        public static Option<MoveResult<Draw, _9Win>> build(Position p0, Position p1, Position p2, Position p3, Position p4, Position p5, Position p6, Position p7, Position p8) { return build(p0, p1, p2, p3, p4, p5, p6, p7).SelectMany(Extensions.GetGame).SelectMany(_8 => _8.move(p8)); }

        static void Main(string[] args)
        {
            Debug.Assert((from _5 in build(Position.NW, Position.N, Position.W, Position.C, Position.SW)
                          select _5.GetWin()).Any());

            Debug.Assert((from _6 in build(Position.NW, Position.N, Position.W, Position.C, Position.NE, Position.S)
                          select _6.GetWin()).Any());

            Debug.Assert((from draw in build(Position.NW, Position.NE, Position.N, Position.W, Position.E, Position.C, Position.SW, Position.S, Position.SE)
                          select draw.GetGame()).Any());

            var _1 = build(Position.NW);
            Debug.Assert((from _2 in _1.move(Position.N)
                          select _2.takeBack() == _1).FirstOrDefault());

            Debug.Assert((from _5 in build(Position.NW, Position.N, Position.NE, Position.W, Position.C).FirstOrDefault().GetGame()
                          from _7 in _5.move(Position.E).FirstOrDefault().GetGame().FirstOrDefault().move(Position.SW).FirstOrDefault().GetWin()
                          select _7.takeBack().takeBack() == _5).FirstOrDefault());

            new UserInterfaceState(Game.Game0, Console.Out, Console.In).Unfold(gameLoop).Last();
        }
    }
}