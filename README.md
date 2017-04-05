# Screenshotter
Windows標準のSnipping Tool風にスクリーンショットを取得し、メッセージとともにツイートします。  
このソフトウェアは、[come25136/MyScreenshotAssistant2](https://github.com/come25136/MyScreenshotAssistant2)をインスパイアしています。
　  
　  
## Features
- スクリーンショット取得からツイートまですべてこれ一つで行えます。
- 画面全体のスクリーンショットを取得してからトリミングするので、タイミングを逃しません。

## Before Building
- /__Private.cs
```cs
namespace Screenshotter
{
    public static class __Private
    {
        public static const string ConsumerKey = "[YOUR CONSUMER KEY]";
        public static const string ConsumerSecret = "[YOUR CONSUMER SECRET]";
    }
}
```

## Licenses
- CoreTweet
  > The MIT License (MIT)  
  >   
  > CoreTweet - A .NET Twitter Library supporting Twitter API 1.1  
  > Copyright (c) 2013-2016 CoreTweet Development Team  
  > https://github.com/CoreTweet/CoreTweet/blob/master/LICENSE

- Newtonsoft.Json
  > The MIT License (MIT)  
  >  
  > Copyright (c) 2007 James Newton-King  
  > https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md

- Global System Hooks
  > The Code Project Open License (CPOL)
  >   
  > © 2004-2005 Michael Kennedy  
  > https://www.codeproject.com/info/cpol10.aspx

- WPF Loaders
  > The MIT License (MIT)  
  >   
  > Copyright (c) 2015 Mickael GOETZ  
  > https://github.com/MrMitch/WPF-Loaders/blob/master/MrMitch.Loaders/LICENSE.txt
