/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2020 Foorack
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UdonSharp;
using UnityEngine;

namespace UdonXMLParser
{
    public class UdonXML_Parser : UdonSharpBehaviour
    {

        private int state = 0;
        private int level = 0;
        private bool isSpecialData = false;
        private bool isWithinCDATA = false;
        private bool isWithinQuotes = false;
        private bool isSelfClosingNode = false;
        private bool hasNodeNameEnded = false;
        private bool hasTagSplitOccured = false; // means the = between the name and the value

        private object[] data;

        // Position to know where we are in the tree.
        private int[] position = new int[0];

        private string nodeName = "";
        private string tagName = "";
        private string tagValue = "";
        private string[] tagNames = new string[0];
        private string[] tagValues = new string[0];

        private char[] input;
        private UdonXML_Callback callback;

        private int updateLoopIttr = 0;
        private bool ready = false;
        private string callbackId;

        void Start()
        {
            data = GenerateEmptyStruct();
            data[0] = "UdonXMLRoot";
        }

        [SerializeField] int runPerSec = 8;
        [SerializeField] int charsPerRun = 100;

        private float dT = 0;
        void Update()
        {
            dT += Time.deltaTime;
            if (dT > 1.0F / runPerSec)
            {
                dT = 0;
                if (ready)
                {
                    for (var j = updateLoopIttr; updateLoopIttr < input.Length && updateLoopIttr <= charsPerRun + j; updateLoopIttr++)
                    {
                        updateLoopIttr = Parse(updateLoopIttr);
                    }

                    if (updateLoopIttr >= input.Length)
                    {
                        callback.OnUdonXMLParseEnd(data, callbackId);
                        Destroy(this.gameObject);
                    }
                    else
                    {
                        callback.OnUdonXMLIteration(updateLoopIttr, input.Length);
                    }
                }
            }
        }


        private object[] GenerateEmptyStruct()
        {
            var emptyStruct = new object[4];
            emptyStruct[0] = ""; // nodeName
            var attr = new object[2];
            attr[0] = new string[0];
            attr[1] = new string[0];
            emptyStruct[1] = attr;
            emptyStruct[2] = new object[0];
            emptyStruct[3] = ""; // nodeValue

            return emptyStruct;
        }

        private string[] AddLastToStringArray(string[] a, string b)
        {
            var n = new string[a.Length + 1];
            for (var i = 0; i != a.Length; i++)
            {
                n[i] = a[i];
            }

            n[a.Length] = b;
            return n;
        }

        private object[] AddLastToObjectArray(object[] a, object b)
        {
            var n = new object[a.Length + 1];
            for (var i = 0; i != a.Length; i++)
            {
                n[i] = a[i];
            }

            n[a.Length] = b;
            return n;
        }

        private int[] AddLastToIntegerArray(int[] a, int b)
        {
            var n = new int[a.Length + 1];
            for (var i = 0; i != a.Length; i++)
            {
                n[i] = a[i];
            }

            n[a.Length] = b;
            return n;
        }

        private int[] RemoveFirstIntegerArray(int[] a)
        {
            var n = new int[a.Length - 1];
            for (var i = 0; i != a.Length - 1; i++)
            {
                n[i] = a[i + 1];
            }

            return n;
        }

        private int[] RemoveLastIntegerArray(int[] a)
        {
            var n = new int[a.Length - 1];
            for (var i = 0; i != a.Length - 1; i++)
            {
                n[i] = a[i];
            }

            return n;
        }

        private object[] FindCurrentLevel(object[] data, int[] position)
        {
            if (position.Length == 0)
            {
                return data;
            }

            var current = data;

            // [ 1, 0, 1]
            while (position.Length != 0)
            {
                current = (object[])((object[])current[2])[position[0]];
                position = RemoveFirstIntegerArray(position);
            }

            return current;
        }
        private int Parse(int i)
        {
            char c = input[i];
            string pos = "";
            for (int n = 0; n != position.Length; n++)
            {
                pos += position[n] + ">";
            }

#if UDONXML_DEBUG
            Debug.Log(state + " " + level + " " + c + "   " + pos);
#endif

            if (state == 0)
            {
                if (
                    input[i + 0] == '<' &&
                    input[i + 1] == '!' &&
                    input[i + 2] == '[' &&
                    input[i + 3] == 'C' &&
                    input[i + 4] == 'D' &&
                    input[i + 5] == 'A' &&
                    input[i + 6] == 'T' &&
                    input[i + 7] == 'A' &&
                    input[i + 8] == '['
                )
                {
                    isWithinCDATA = true;
                    i += "<![CDATA[".Length - 1;
                }
                else if (c == '<' && !isWithinCDATA)
                {
                    {
                        isSpecialData = false;
                        isWithinQuotes = false;
                        isSelfClosingNode = false;
                        hasNodeNameEnded = false;
                        hasTagSplitOccured = false;
                        nodeName = "";
                        tagNames = new string[0];
                        tagValues = new string[0];
                        state = 1;
                    }
                }
                else if (isWithinCDATA && c == ']' && input[i + 1] == ']')
                {
                    isWithinCDATA = false;
                    i += 2;
                }
                else
                {
                    object[] s = FindCurrentLevel(data, position);
                    s[3] = (string)s[3] + c;
                }
            }
            else if (state == 1)
            {
                if (c == '/')
                {
                    state = 2;
                }
                else
                {
                    if (c == '?' || c == '!')
                    {
                        isSpecialData = true;
                    }

                    nodeName += c + "";
                    state = 3;
                }
            }
            else if (state == 2)
            {
                if (c == '>')
                {
                    level--;
                    position = RemoveLastIntegerArray(position);

                    state = 0;
#if UDONXML_DEBUG
                    Debug.Log("CLOSED TAG : " + nodeName);
#endif
                }
                else
                {
                    nodeName += c + "";
                }
            }
            else if (state == 3)
            {
                if (c == '>' && !isWithinQuotes)
                {
#if UDONXML_DEBUG
                    Debug.Log("OPENED TAG : " + nodeName);
#endif
                    state = 0;
                    tagName = "";
                    tagValue = "";

                    var s = FindCurrentLevel(data, position);
                    position = AddLastToIntegerArray(position, ((object[])s[2]).Length);

                    s[2] = AddLastToObjectArray((object[])s[2], GenerateEmptyStruct());
                    var children = (object[])s[2];
                    var child = (object[])children[children.Length - 1];

                    child[0] = nodeName;
                    var attr = (object[])child[1];
                    attr[0] = tagNames;
                    attr[1] = tagValues;

                    if (isSelfClosingNode || isSpecialData)
                    {
                        position = RemoveLastIntegerArray(position);
#if UDONXML_DEBUG
                        Debug.Log("SELF-CLOSED TAG : " + nodeName);
#endif
                    }

                    if (!isSelfClosingNode && !isSpecialData)
                    {
                        level++;
                    }
                }

                else if (c == '/' && !isWithinQuotes)
                {
                    isSelfClosingNode = true;
                }
                else if (c == '"')
                {
                    if (isWithinQuotes)
                    {
                        // Add tag
                        if (tagName.Trim().Length != 0)
                        {
                            tagNames = AddLastToStringArray(tagNames, tagName.Trim());
                            tagValues = AddLastToStringArray(tagValues, tagValue);
                            tagName = "";
                            tagValue = "";
                            hasTagSplitOccured = false;
                        }
                    }

                    isWithinQuotes = !isWithinQuotes;
                }
                else if (c == '=' && !isWithinQuotes)
                {
                    hasTagSplitOccured = true;
                }
                else
                {
                    if (c == ' ' && !hasNodeNameEnded)
                    {
                        hasNodeNameEnded = true;
                        var nodeNameLow = nodeName.ToLower();
                        if (nodeNameLow == "area" || nodeNameLow == "base" || nodeNameLow == "br" ||
                            nodeNameLow == "embed" || nodeNameLow == "hr" || nodeNameLow == "iframe" ||
                            nodeNameLow == "img" || nodeNameLow == "input" || nodeNameLow == "link" ||
                            nodeNameLow == "meta" || nodeNameLow == "param" || nodeNameLow == "source" ||
                            nodeNameLow == "track")
                        {
                            isSelfClosingNode = true;
                        }
                    }

                    if (hasNodeNameEnded)
                    {
                        // if tag name or tag value
                        if (hasTagSplitOccured)
                        {
                            tagValue += c + "";
                        }
                        else
                        {
                            // i.e. bordered in <table>, or html in <doctype>, sometimes they don't have values
                            if (c == ' ')
                            {
                                // Add tag
                                if (tagName.Trim().Length != 0)
                                {
                                    tagNames = AddLastToStringArray(tagNames, tagName.Trim());
                                    tagValues = AddLastToStringArray(tagValues, null);
                                    tagName = "";
                                }
                            }
                            else
                            {
                                tagName += c + "";
                            }
                        }
                    }
                    else
                    {
                        nodeName += c + "";
                    }
                }
            }
            return i;
        }
        /**
        * Loads an XML structure into memory by parsing the provided input.
        *
        * Returns null in case of parse failure.
        */
        public void LoadXml(string input, UdonXML_Callback callback, string callbackId)
        {
            this.callback = callback;
            this.input = input.ToCharArray();
            this.callbackId = callbackId;
            ready = true;
        }
    }
}