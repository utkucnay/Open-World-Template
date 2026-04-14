namespace Glai.ECS
{
    public interface IQueryJob<T1>
        where T1 : unmanaged, IComponent
    {
        void Execute(ref T1 c1);
    }

    public interface IQueryJob<T1, T2>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        void Execute(ref T1 c1, ref T2 c2);
    }

    public interface IQueryJob<T1, T2, T3>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        void Execute(ref T1 c1, ref T2 c2, ref T3 c3);
    }

    public interface IQueryJob<T1, T2, T3, T4>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        void Execute(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
    }

    public interface IQueryJob<T1, T2, T3, T4, T5>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        void Execute(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);
    }

    public interface IQueryJob<T1, T2, T3, T4, T5, T6>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        void Execute(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6);
    }

    public interface IQueryJob<T1, T2, T3, T4, T5, T6, T7>
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
    {
        void Execute(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7);
    }
}
