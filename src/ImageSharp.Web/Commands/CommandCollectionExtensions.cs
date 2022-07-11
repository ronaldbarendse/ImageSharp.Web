// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.Web.Processors;

namespace SixLabors.ImageSharp.Web.Commands
{
    /// <summary>
    /// Extension methods for <see cref="CommandCollectionExtensions"/>.
    /// </summary>
    public static class CommandCollectionExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key or the default value.
        /// </summary>
        /// <param name="collection">The collection instance.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key or the default value.</returns>
        public static string GetValueOrDefault(this CommandCollection collection, string key)
        {
            collection.TryGetValue(key, out KeyValuePair<string, string> result);
            return result.Value;
        }

        /// <summary>
        /// Gets the known commands from the processors.
        /// </summary>
        /// <param name="processors">A collection of <see cref="IImageWebProcessor"/> instances used to get the known commands.</param>
        /// <returns>The collection of known commands gathered from the processors.</returns>
        internal static HashSet<string> GetKnownCommands(this IEnumerable<IImageWebProcessor> processors)
        {
            HashSet<string> knownCommands = new(StringComparer.OrdinalIgnoreCase);
            foreach (IImageWebProcessor processor in processors)
            {
                foreach (string command in processor.Commands)
                {
                    knownCommands.Add(command);
                }
            }

            return knownCommands;
        }

        /// <summary>
        /// Removes any unknown commands from the command collection.
        /// </summary>
        /// <param name="collection">The command collection.</param>
        /// <param name="knownCommands">The known commands.</param>
        internal static void RemoveUnknownCommands(this CommandCollection collection, HashSet<string> knownCommands)
        {
            if (collection?.Count > 0)
            {
                // Strip out any unknown commands, if needed.
                var keys = new List<string>(collection.Keys);
                for (int i = keys.Count - 1; i >= 0; i--)
                {
                    if (!knownCommands.Contains(keys[i]))
                    {
                        collection.RemoveAt(i);
                    }
                }
            }
        }
    }
}
