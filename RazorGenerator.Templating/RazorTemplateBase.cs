using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RazorGenerator.Templating
{
    public class RazorTemplateBase
    {
        public RazorTemplateBase()
        {
            this.culture               = CultureInfo.CurrentCulture;
            this.generatingEnvironment = new StringBuilder();
            this.output                = new StringWriter(this.generatingEnvironment); // StringWriter does not own any unmanaged resources, there is no need to call Dispose nor make RazorTemplateBase implement IDisposable.
        }

        private readonly StringBuilder generatingEnvironment;
        private readonly TextWriter    output;

        private string renderedContent;

        public RazorTemplateBase Layout { get; set; }

        private CultureInfo culture;
        public CultureInfo Culture
        {
            get { return this.culture; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value != this.culture)
                {
                    this.culture = value;
                }
            }
        }

        //////////////////////////////////////////

        public virtual void Execute()
        {
        }

        public void WriteLiteral(string textToAppend)
        {
            if (String.IsNullOrEmpty(textToAppend)) return;

            this.generatingEnvironment.Append(textToAppend);
        }

        public IRawString Raw(string value)
        {
            return new RawString(value);
        }

        public void Write(string value)
        {
            this.WriteEncoded(value);
        }

        public void Write(object value)
        {
            if (value == null)
            {
                return;
            }
            else if (value is IRawString rawValue) // checking for IRawString despite the explicit `Write(IRawString)` overload is to ensure this method handles cases where compiler cannot choose the best overload at compile-time.
            {
                this.Write( rawValue );
            }
            else if (value is IConvertible convertible)
            {
                String text = convertible.ToString(this.Culture);
                this.WriteEncoded(text);
            }
            else
            {
                String text = Convert.ToString(value, this.Culture);
                this.WriteEncoded(text);
            }
        }

        public void Write(IRawString value)
        {
            if (value == null) return;

            String rawValue = value.ToRawString();
            this.WriteLiteral(rawValue);
        }

        // Because this is a generic method constrained on IConvertible, there is no need to have separate int/long/float overloads of `Write(value)` anymore (and Write(object) supplants Write(bool)).
        [CLSCompliant( false )]
        public void Write<T>(T value)
            where T : struct, IConvertible
        {
            String text = value.ToString(this.Culture);
            this.WriteEncoded(text);
        }

        [CLSCompliant( false )]
        public void Write<T>(T? value)
            where T : struct, IConvertible
        {
            if( value.HasValue )
            {
                String text = value.Value.ToString(this.Culture);
                this.WriteEncoded(text);
            }
        }

        /// <summary>By default this method does not actually encode <paramref name="value"/> before being written to the output (it just calls <c>WriteLiteral(String)</c> directly). Subclasses can override this method to encode values when appropriate (e.g. a HTML report subclass implementation might call WebUtility.HtmlEncode)</summary>
        public virtual void WriteEncoded(string value)
        {
            if (String.IsNullOrEmpty(value)) return;

            this.WriteLiteral(value);
        }

        #region Razor lifecycle methods

        public string RenderBody()
        {
            return this.renderedContent;
        }

        public string TransformText()
        {
            this.Execute();

            if (this.Layout != null)
            {
                this.Layout.renderedContent = this.generatingEnvironment.ToString();
                return this.Layout.TransformText();
            }
            else
            {
                return this.generatingEnvironment.ToString();
            }
        }

        public void Clear()
        {
            this.generatingEnvironment.Clear();

            if (this.Layout != null)
            {
                this.Layout.renderedContent = "";
            }
        }

        #endregion

        #region Sections

        private readonly Stack<Dictionary<string, Action>> sectionWritersStack = new Stack<Dictionary<string, Action>>();

        private Dictionary<string, Action> GetSectionWriters()
        {
            return this.sectionWritersStack.Peek();
        }

        private Dictionary<string, Action> GetPreviousSectionWriters()
        {
            Dictionary<string, Action> current  = this.sectionWritersStack.Pop();
            Dictionary<string, Action> previous = this.sectionWritersStack.Count > 0 ? this.sectionWritersStack.Peek() : null;
            this.sectionWritersStack.Push(current);
            return previous;
        }

        public bool IsSectionDefined(string name)
        {
            Dictionary<string, Action> previous = this.GetPreviousSectionWriters();
            if (previous == null) throw new InvalidOperationException("Cannot query sections in the current state.");

            return previous.ContainsKey(name);
        }

        public void DefineSection(string name, Action action)
        {
            Dictionary<string, Action> writers = this.GetSectionWriters();

            if (writers.ContainsKey(name)) throw new InvalidOperationException( "Section \"" + name + "\" is already defined." );

            writers[name] = action;
        }

        public RazorResult RenderSection(string name)
        {
             return this.RenderSection(name, required: true);
        }

        public RazorResult RenderSection(string name, bool required)
        {
            Dictionary<string, Action> previous = this.GetPreviousSectionWriters();

            if (previous == null) throw new InvalidOperationException("Cannot render sections in the current state.");

            if (previous.ContainsKey(name))
            {
                Action<TextWriter> action = delegate(TextWriter wtr)
                {
                    if (this.renderedSections.Contains(name)) throw new InvalidOperationException("Section \"" + name + "\" is already rendered.");

                    Action writer = previous[name];
                    Dictionary<string, Action> top = this.sectionWritersStack.Pop();

                    bool pushed = false;
                    try
                    {
                        if (this.output != wtr)
                        {
                            this.outputStack.Push(wtr);
                            pushed = true;
                        }

                        writer();
                    }
                    finally
                    {
                        if (pushed) this.outputStack.Pop();
                    }

                    this.sectionWritersStack.Push(top);
                    this.renderedSections.Add(name);
                };

                return new RazorResult( action );
            }
            else if (required)
            {
                throw new InvalidOperationException("Required section \"" + name + "\" is not defined.");
            }
            else
            {
                return null;
            }
        }

        private readonly Stack<TextWriter> outputStack = new Stack<TextWriter>();
        private readonly HashSet<string> renderedSections = new HashSet<string>();

        #endregion

        public void WriteLiteralTo(TextWriter writer, string text)
        {
            writer.Write(text);
        }

        public void WriteTo(TextWriter writer, string value)
        {
            this.WriteEncoded(value);
        }

        public void WriteTo(TextWriter writer, object value)
        {
            if (value == null)
            {
                return;
            }
            else if (value is IRawString rawValue) // checking for IRawString despite the explicit `Write(IRawString)` overload is to ensure this method handles cases where compiler cannot choose the best overload at compile-time.
            {
                this.WriteTo(writer, rawValue);
            }
            else if (value is IConvertible convertible)
            {
                String text = convertible.ToString(this.Culture);
                this.WriteEncodedTo(writer, text);
            }
            else
            {
                String text = Convert.ToString(value, this.Culture);
                this.WriteEncodedTo(writer, text);
            }
        }

        public void WriteTo(TextWriter writer,IRawString value)
        {
            if (value == null) return;

            String rawValue = value.ToRawString();
            this.WriteLiteralTo(writer, rawValue);
        }

        // Because this is a generic method constrained on IConvertible, there is no need to have separate int/long/float overloads of `Write(value)` anymore (and Write(object) supplants Write(bool)).
        [CLSCompliant( false )]
        public void WriteTo<T>(TextWriter writer, T value)
            where T : struct, IConvertible
        {
            String text = value.ToString(this.Culture);
            this.WriteEncodedTo(writer,text);
        }

        [CLSCompliant( false )]
        public void WriteTo<T>(TextWriter writer, T? value)
            where T : struct, IConvertible
        {
            if( value.HasValue )
            {
                String text = value.Value.ToString(this.Culture);
                this.WriteEncodedTo(writer,text);
            }
        }

        public virtual void WriteEncodedTo(TextWriter writer, string value)
        {
            if (String.IsNullOrEmpty(value)) return;

            this.WriteLiteralTo(writer, value);
        }

        // WriteAttribute is used by Razor runtime v2 and v3.

        public void WriteAttribute(string name, Tuple<string, int> prefix, Tuple<string, int> suffix, params object[] fragments)
        {
            WriteAttributeTo(this.output, name, prefix, suffix, fragments);
        }

        public void WriteAttributeTo(TextWriter writer, string name, Tuple<string, int> prefix, Tuple<string, int> suffix, params object[] fragments)
        {
            // For sake of compatibility, this implementation is adapted from
            // System.Web.WebPages.WebPageExecutingBase as found in ASP.NET
            // web stack release 3.2.2:
            // https://github.com/ASP-NET-MVC/aspnetwebstack/releases/tag/v3.2.2

            #region Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.

            // Licensed under the Apache License, Version 2.0 (the "License")
            // may not use this file except in compliance with the License. You may
            // obtain a copy of the License at
            //
            // http://www.apache.org/licenses/LICENSE-2.0
            //
            // Unless required by applicable law or agreed to in writing, software
            // distributed under the License is distributed on an "AS IS" BASIS,
            // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
            // implied. See the License for the specific language governing permissions
            // and limitations under the License.

            #endregion

            if (fragments.Length == 0)
            {
                WriteLiteralTo(writer, prefix.Item1);
                WriteLiteralTo(writer, suffix.Item1);
            }
            else
            {
                bool first = true;
                bool wroteSomething = false;
                foreach (object fragment in fragments)
                {
                    var sf = fragment as Tuple<Tuple<string, int>, Tuple<string, int>, bool>;
                    var of = sf == null ? (Tuple<Tuple<string, int>, Tuple<object, int>, bool>) fragment : null;

                    string ws      = sf != null ? sf.Item1.Item1 : of.Item1.Item1;
                    bool   literal = sf != null ? sf.Item3       : of.Item3;
                    object val     = sf != null ? sf.Item2.Item1 : of.Item2.Item1;

                    if (val == null) continue; // nothing to write

                    // The special cases here are that the value we're writing might already be a string, or that the
                    // value might be a bool. If the value is the bool 'true' we want to write the attribute name instead
                    // of the string 'true'. If the value is the bool 'false' we don't want to write anything.
                    //
                    // Otherwise the value is another object (perhaps an IHtmlString), and we'll ask it to format itself.
                    string str;
                    var flag = val as bool?;

                    switch (flag)
                    {
                        case true:
                            str = name;
                            break;
                        case false:
                            continue;
                        default:
                            str = val as string;
                            break;
                    }

                    if (first)
                    {
                        WriteLiteralTo(writer, prefix.Item1);
                        first = false;
                    }
                    else
                    {
                        WriteLiteralTo(writer, ws);
                    }

                    // The extra branching here is to ensure that we call the Write*To(string) overload when
                    // possible.
                    if (literal && str != null)
                    {
                        WriteLiteralTo(writer, str);
                    }
                    else if (literal)
                    {
                        WriteLiteralTo(writer, (string)val);
                    }
                    else if (str != null)
                    {
                        WriteTo(writer, str);
                    }
                    else
                    {
                        WriteTo(writer, val);
                    }

                    wroteSomething = true;
                }//foreach

                if (wroteSomething)
                {
                    WriteLiteralTo(writer, suffix.Item1);
                }
                    
            }
        }
    }

    /// <summary>Interface for types that represent text that must not be encoded before being written to the output.</summary>
    public interface IRawString
    {
        string ToRawString();
    }

    /// <summary>Wraps a string value that must not be encoded before being written to the output. Implements IRawString.</summary>
    public class RawString : IRawString
    {
        public RawString(String value)
        {
            this.Value = value;
        }

        public string Value { get; }

        public string ToRawString()
        {
            return this.Value;
        }

        public override string ToString()
        {
            return this.ToRawString();
        }
    }

    public class RazorResult : IRawString
    {
        private readonly Action<TextWriter> action;

        public RazorResult(Action<TextWriter> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public string ToRawString()
        {
            using( StringWriter wtr = new StringWriter( CultureInfo.InvariantCulture ) )
            {
                this.action( wtr );
                return wtr.ToString();
            }
        }

        public void WriteTo(TextWriter writer)
        {
            if( writer == null ) throw new ArgumentNullException(nameof(writer));

            this.action( writer );
        }
    }
}

// Due to a design-bug in Razor, whenever a `@helper` is used, the Razor compiler is hardcoded to use `System.Web.WebPages.HelperResult` instead of allowing users to set their own.
// As a fix-hack, this assembly will now provide a System.Web.WebPages.HelperResult type:
namespace System.Web.WebPages
{
    using RazorGenerator.Templating;

    public class HelperResult : RazorResult
    {
        public HelperResult(Action<TextWriter> action)
            : base(action)
        {

        }
    }
}