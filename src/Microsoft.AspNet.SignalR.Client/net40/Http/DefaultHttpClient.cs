// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET40

#elif NET45 || NETSTANDARD1_3 || NETSTANDARD2_0
// See Http/DefaultHttpClient.cs
#else 
#error Unsupported target framework.
#endif
