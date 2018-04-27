using System;

namespace QuickTunnel
{
	public class Box<T>
	{
		public T Value;

		public Box() : this(default(T)) { }
		public Box(T value) => Value = value;

		public static implicit operator T (Box<T> box) => box.Value;

		[Obsolete("Assigning as `box = newValue` will break existing references, change to `box.Value = newValue` to preserve the reference, or to `box = new Box<T>(newValue)` to create a new reference.")]
		public static implicit operator Box<T> (T value) => new Box<T>(value);
	}
}
