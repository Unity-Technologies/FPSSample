using System;
using System.Collections.Generic;
using System.Collections.Concurrent;


public class SimplePool<T>
{
	private readonly ConcurrentBag<T> _objects;
	private readonly Func<T> _objectGenerator;

	//example usage 
	// var pool = new ObjectPool<ExampleObject>(() => new ExampleObject());
	public SimplePool(Func<T> objectGenerator)
	{
		_objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
		_objects = new ConcurrentBag<T>();
	}

	public T Get() => _objects.TryTake(out T item) ? item : _objectGenerator();

	public void Return(T item) => _objects.Add(item);
}

