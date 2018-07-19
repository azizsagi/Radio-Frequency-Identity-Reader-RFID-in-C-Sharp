using System;
using System.Globalization;
using System.Configuration;
using System.Collections;
using System.Windows.Forms;
using System.Xml;


namespace ToolHelp.Configuration
{
	/// <summary>
	/// This class encapsulates a map of named values read from a configuration
	/// file.
	/// </summary>
	internal class AppSettingsReader : IDictionary
	{
		/// <summary>The internally used hastable for name/value configuration setting pairs</summary>
		private IDictionary appSettingsDictionary = null;

    #region Constructor
		/// <summary>
		/// Gets configuration settings in the &lt;appSettings&gt; Element configuration section.
		/// </summary>
		/// <param name="configFile">the path to the configuration-file.</param>
		public AppSettingsReader(string configFile)
		{
			this.m_ConfigFile = configFile;
			LoadConfig();
		}
		/// <summary>
		/// Gets configuration settings in the &lt;appSettings&gt; Element configuration section.
		/// </summary>
		public AppSettingsReader()
		{
		}
    #endregion
	
    #region private void LoadConfig()
		/// <summary>
		/// Fill the configuration dictionary (if not already initialized) by loading
		/// it from a configuration file.
		/// </summary>
		private void LoadConfig()
		{
    #region get appSettingsDictionary
			if(this.appSettingsDictionary==null)
			{
				this.appSettingsDictionary = new Hashtable();
				try
				{
					XmlDocument configXml = new XmlDocument();
					XmlTextReader configXmlReader = new XmlTextReader(this.m_ConfigFile);
					configXml.Load(configXmlReader);
					configXmlReader.Close();
					XmlNodeList configNodes = configXml.GetElementsByTagName("appSettings");
					foreach(XmlNode configNode in configNodes)
					{
						if(configNode.LocalName=="appSettings")
						{
							foreach (XmlNode configEntry in configNode.ChildNodes)
							{
								if (configEntry.NodeType == XmlNodeType.Element &&
									configEntry.Name == "add")
								{
									string key = configEntry.Attributes["key"].Value;
									object value = configEntry.Attributes["value"].Value;
									this.appSettingsDictionary.Add(key, value);
								}
							}
						}
					}
				}
				catch
				{
					// Ignore exceptions occuring with the configuration file
				}
			}
    #endregion
		}
    #endregion

    #region public string ConfigFile
		/// <summary>This field holds the value of the ConfigFile.</summary>
		private string m_ConfigFile;

		/// <summary>This property gets/sets the value of the ConfigFile.</summary>
		/// <value>A <see cref="string">string</see> with the value of the ConfigFile.</value>
		/// <remarks>This property gets/sets the value of the ConfigFile.</remarks>
		public string ConfigFile
		{
			get
			{
				return (this.m_ConfigFile);
			}
			set
			{
				this.m_ConfigFile = value;
				LoadConfig();
			}
		}
    #endregion

    #region public object this[ object key ]
		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key whose value to get or set.</param>
		/// <value>An Object with the value associated with the specified key.</value>
		/// <exception cref="ArgumentNullException">key is null.</exception>
		/// <exception cref="NotSupportedException">
		/// The property is set and the AppSettingsCollection is read-only.<br/>
		/// -or- <br/>
		/// The property is set, key does not exist in the collection, and the AppSettingsCollection has a fixed size.<br/>
		/// </exception>
		/// <remarks>
		/// This property provides the ability to access a specific element in the collection by using the following
		/// syntax: <code>myCollection[key]</code> .<br/><br/>
		/// When setting this property, if the specified key already exists in the AppSettingsCollection, the value is replaced;
		/// otherwise, a new element is created. In contrast, the AppSettingsCollection.Add method does not modify existing elements.
		/// </remarks>
		public virtual object this[object key]
		{
			get
			{
				string value = null;
				if(this.appSettingsDictionary!=null )
				{
					value = this.appSettingsDictionary[key] as string;
				}
				return value;
			}
			set
			{
				
			}
		}
    #endregion

    #region public void CopyTo(Array array, int arrayIndex)
		/// <summary>
		/// Copies the AppSettingsCollection elements to a one-dimensional Array instance at the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination of the DictionaryEntry
		/// objects copied from AppSettingsCollection. The Array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		/// <exception cref="ArgumentNullException">array is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero..</exception>
		/// <exception cref="ArgumentException">array is multidimensional.<br/>-or-<br/>
		/// arrayIndex is equal to or greater than the length of array.
		/// <br/>-or-<br/>
		/// The number of elements in the source AppSettingsCollection is greater than the available space from arrayIndex to
		/// the end of the destination array.</exception>
		/// <exception cref="InvalidCastException">The type of the source AppSettingsCollection cannot be cast automatically to
		/// the type of the destination array.</exception>
		/// <remarks>
		/// The elements are copied to the Array in the same order in which the enumerator iterates through the AppSettingsCollection.<br/><br/>
		/// To copy only the keys in the AppSettingsCollection, use <code>AppSettingsCollection.Keys.CopyTo</code> .<br/>
		/// To copy only the values in the AppSettingsCollection, use <code>AppSettingsCollection.Values.CopyTo</code> .
		/// </remarks>
		public void CopyTo(Array array, int arrayIndex)
		{
			appSettingsDictionary.CopyTo(array, arrayIndex);
		}
    #endregion

    #region public int Count
		/// <summary>
		/// Gets the number of key-and-value pairs contained in the AppSettingsCollection.
		/// </summary>
		/// <value>A Integer.</value>
		public int Count
		{
			get
			{
				return appSettingsDictionary.Count;
			}
		}
    #endregion

    #region public bool IsSynchronized
		/// <summary>
		/// Gets a value indicating whether access to the AppSettingsCollection is synchronized (thread-safe).
		/// </summary>
		/// <value>A Boolean.</value>
		/// <remarks>
		/// A AppSettingsCollection can safely support one writer and multiple readers concurrently. To support multiple writers,
		/// all operations must be done through the wrapper returned by the AppSettingsCollection.Synchronized method.<br/><br/>
		/// Enumerating through a collection is intrinsically not a thread-safe procedure. Even when a collection is
		/// synchronized, other threads could still modify the collection, which causes the enumerator to throw an
		/// exception. To guarantee thread safety during enumeration, you can either lock the collection during
		/// the entire enumeration or catch the exceptions resulting from changes made by other threads.
		/// </remarks>
		public bool IsSynchronized
		{
			get
			{
				return appSettingsDictionary.IsSynchronized;
			}
		}
    #endregion

    #region public object SyncRoot
		/// <summary>
		/// Gets an object that can be used to synchronize access to the AppSettingsCollection.
		/// </summary>
		/// <value>An Object that can be used to synchronize access to the AppSettingsCollection.</value>
		/// <remarks>
		/// To create a synchronized version of the AppSettingsCollection, use the <code>AppSettingsCollection.Synchronized</code> method.
		/// However, derived classes can provide their own synchronized version of the AppSettingsCollection using the
		/// <code>AppSettingsCollection.SyncRoot</code> property. The synchronizing code must perform operations on the
		/// <code>AppSettingsCollection.SyncRoot</code> of the AppSettingsCollection, not directly on the AppSettingsCollection. This ensures proper
		/// operation of collections that are derived from other objects. Specifically, it maintains proper
		/// synchronization with other threads that might be simultaneously modifying the AppSettingsCollection object.<br/><br/>
		/// Enumerating through a collection is intrinsically not a thread-safe procedure. Even when a collection is
		/// synchronized, other threads could still modify the collection, which causes the enumerator to throw an
		/// exception. To guarantee thread safety during enumeration, you can either lock the collection during the
		/// entire enumeration or catch the exceptions resulting from changes made by other threads.
		/// </remarks>
		public object SyncRoot
		{
			get
			{
				return appSettingsDictionary.SyncRoot;
			}
		}
    #endregion

    #region public void Add(object key, object value)
		/// <summary>
		/// Adds an element with the specified key and value into the AppSettingsCollection.
		/// </summary>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add.</param>
		/// <remarks>
		/// An object that has no correlation between its state and its hash code value should typically
		/// not be used as the key. For example, String objects are better than StringBuilder objects for
		/// use as keys.<br/><br/>
		/// The AppSettingsCollection.Item property can also be used to add new elements by setting the value of a key
		/// that does not exist in the AppSettingsCollection. For example:
		/// <code>myCollection["myNonexistentKey"] = myValue</code> .
		/// However, if the specified key already exists in the AppSettingsCollection, setting the AppSettingsCollection.Item
		/// property overwrites the old value. In contrast, the AppSettingsCollection.Add method does not modify
		/// existing elements.
		/// </remarks>
		/// <exception cref="ArgumentNullException">key is null.</exception>
		/// <exception cref="ArgumentException">An element with the same key already exists in the AppSettingsCollection.</exception>
		/// <exception cref="NotSupportedException">
		/// The AppSettingsCollection is read-only.<br/>
		/// -or- <br/>
		/// The AppSettingsCollection has a fixed size.<br/>
		/// </exception>
		public void Add(object key, object value)
		{
			appSettingsDictionary.Add(key, value);
		}
    #endregion

    #region public void Clear()
		/// <summary>
		/// Removes all elements from the AppSettingsCollection.
		/// </summary>
		/// <remarks>AppSettingsCollection.Count is set to zero. The capacity remains unchanged. </remarks>
		/// <exception cref="NotSupportedException">The AppSettingsCollection is read-only.</exception>
		public void Clear()
		{
			appSettingsDictionary.Clear();
		}
    #endregion

    #region public bool Contains(object key)
		/// <summary>
		/// Determines whether the AppSettingsCollection contains a specific key.
		/// </summary>
		/// <param name="key">The key to locate in the AppSettingsCollection.</param>
		/// <returns><b>true</b> if the AppSettingsCollection contains an element with the specified key; otherwise, <b>false</b>.</returns>
		/// <remarks>
		/// This implementation is close to O(1) in most cases.<br/><br/>
		/// AppSettingsCollection.Contains implements IDictionary.Contains. It behaves exactly as AppSettingsCollection.ContainsKey.
		/// </remarks>
		/// <exception cref="ArgumentNullException">key is null.</exception>
		/// <value>A bool.</value>
		public bool Contains(object key)
		{
			return appSettingsDictionary.Contains(key);
		}
    #endregion

    #region public IEnumerator GetEnumerator()
		/// <summary>
		/// Returns an IDictionaryEnumerator that can iterate through the AppSettingsCollection.
		/// </summary>
		/// <returns>An IDictionaryEnumerator for the AppSettingsCollection.</returns>
		/// <remarks>
		/// Enumerators only allow reading the data in the collection. Enumerators cannot be used to modify the underlying
		/// collection.<br/><br/>
		/// Initially, the enumerator is positioned before the first element in the collection. <code>IEnumerator.Reset</code> also
		/// brings the enumerator back to this position. At this position, calling <code>IEnumerator.Current</code> throws an exception.
		/// Therefore, you must call <code>IEnumerator.MoveNext</code> to advance the enumerator to the first element of the collection
		/// before reading the value of <code>IEnumerator.Current</code>.<br/><br/>
		/// <code>IEnumerator.Current</code> returns the same object until either <code>IEnumerator.MoveNext</code> or <code>IEnumerator.Reset</code> is called.
		/// <code>IEnumerator.MoveNext</code> sets IEnumerator.Current to the next element.<br/><br/>
		/// After the end of the collection is passed, the enumerator is positioned after the last element in the
		/// collection, and calling <code>IEnumerator.MoveNext</code> returns false. If the last call to IEnumerator.MoveNext
		/// returned false, calling IEnumerator.Current throws an exception. To set IEnumerator.Current to the first
		/// element of the collection again, you can call <code>IEnumerator.Reset</code> followed by
		/// <code>IEnumerator.MoveNext</code>.<br/><br/>
		/// An enumerator remains valid as long as the collection remains unchanged. If changes are made to the
		/// collection, such as adding, modifying or deleting elements, the enumerator is irrecoverably invalidated
		/// and the next call to <code>IEnumerator.MoveNext</code> or <code>IEnumerator.Reset</code> throws an
		/// InvalidOperationException. If the collection is modified between <code>IEnumerator.MoveNext</code> and
		/// <code>IEnumerator.Current</code>, <code>IEnumerator.Current</code> will return the element that it is
		/// set to, even if the enumerator is already invalidated.<br/><br/>
		/// The enumerator does not have exclusive access to the collection; therefore, enumerating through a collection
		/// is intrinsically not a thread-safe procedure. Even when a collection is synchronized, other threads could
		/// still modify the collection, which causes the enumerator to throw an exception. To guarantee thread safety
		/// during enumeration, you can either lock the collection during the entire enumeration or catch the exceptions
		/// resulting from changes made by other threads.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// If changes are made to the collection, such as adding, modifying or deleting elements, the enumerator
		/// is irrecoverably invalidated and the next call to <code>IEnumerator.MoveNext</code> or
		/// <code>IEnumerator.Reset</code> throws an InvalidOperationException.
		/// </exception>
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return null; //appSettingsDictionary.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return null; //appSettingsDictionary.GetEnumerator();
		}
    #endregion

    #region public bool IsFixedSize
		/// <summary>
		/// Gets a value indicating whether the AppSettingsCollection has a fixed size.
		/// </summary>
		/// <value>A Boolean.</value>
		/// <remarks>
		/// A collection with a fixed size does not allow the addition or removal of elements after the collection
		/// is created, but it allows the modification of existing elements.
		/// </remarks>
		public bool IsFixedSize
		{
			get
			{
				return appSettingsDictionary.IsFixedSize;
			}
		}
    #endregion

    #region public bool IsReadOnly
		/// <summary>
		/// Gets a value indicating whether the AppSettingsCollection is read-only.
		/// </summary>
		/// <value>A Boolean.</value>
		/// <remarks>
		/// A collection that is read-only does not allow the addition, removal, or modification of elements
		/// after the collection is created.
		/// </remarks>
		public bool IsReadOnly
		{
			get
			{
				return appSettingsDictionary.IsReadOnly;
			}
		}
    #endregion

    #region public ICollection Keys
		/// <summary>
		/// Gets an ICollection containing the keys in the AppSettingsCollection.
		/// </summary>
		/// <value>An ICollection containing the keys in the AppSettingsCollection.</value>
		/// <remarks>
		/// The order of the keys in the ICollection is unspecified, but it is the same order as the associated values
		/// in the ICollection returned by the AppSettingsCollection.Values method.<br/><br/>
		/// The returned ICollection is a reference to the original AppSettingsCollection, not a static copy. Therefore, changes to
		/// the AppSettingsCollection continue to be reflected in the ICollection.
		/// </remarks>
		public ICollection Keys
		{
			get
			{
				return appSettingsDictionary.Keys;
			}
		}
    #endregion

    #region public ICollection Values
		/// <summary>
		/// Gets an ICollection containing the values in the AppSettingsCollection.
		/// </summary>
		/// <value>An ICollection containing the values in the AppSettingsCollection.</value>
		/// <remarks>
		/// The order of the values in the ICollection is unspecified, but it is the same order as the associated keys
		/// in the ICollection returned by the AppSettingsCollection.Keys method.<br/><br/>
		/// The returned ICollection is a reference to the original AppSettingsCollection, not a static copy. Therefore, changes to
		/// the AppSettingsCollection continue to be reflected in the ICollection.
		/// </remarks>
		public ICollection Values
		{
			get
			{
				return appSettingsDictionary.Values;
			}
		}
    #endregion

    #region public void Remove(object key)
		/// <summary>
		/// Removes the element with the specified key from the AppSettingsCollection.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		/// <exception cref="ArgumentNullException">key is null.</exception>
		/// <exception cref="NotSupportedException">The AppSettingsCollection is read-only.<br/>-or-<br/>The AppSettingsCollection has a fixed size.</exception>
		/// <remarks>
		/// If the AppSettingsCollection does not contain an element with the specified key, the AppSettingsCollection remains unchanged.
		/// No exception is thrown.
		/// </remarks>
		public void Remove(object key)
		{
			appSettingsDictionary.Remove(key);
		}
    #endregion

	}
}
