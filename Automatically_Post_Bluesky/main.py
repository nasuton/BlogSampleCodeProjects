from atproto import Client, models, client_utils
import requests
from bs4 import BeautifulSoup
import re

# blueskyアカウントID
bluesky_account = "*******.bsky.social"
# blueskyのパスワード
bluesky_pwd = "******"
url = "https://nasuton.net/blog/trying_newfeatures_csharp13/"
title = "C# 13の新機能を使ってみる"
hash_tags =["CSharp"]

def get_description(url:str):
    response = None
    try:
        # requestsで対象のURLに対してGET
        response = requests.get(url, timeout=60)
    except:
        return '', ''
    if response.status_code != 200:
        return '', ''

    # responseに含まれるテキストデータを、HTMLパーサで処理
    soup = BeautifulSoup(response.text, 'html.parser')
    # metaタグdescription内のテキストを取得
    og_desc_tag = soup.find('meta', attrs={'property': 'og:description'})
    if og_desc_tag != None:
        og_description = og_desc_tag.get('content', '').strip() if og_desc_tag else ''

    return og_description

def extract_url_byte_positions(text, *, aggressive: bool, encoding='UTF-8'):
    """
    aggressiveがFalseの場合、「http」または「https」で始まるリンクのみが検出されます
    """
    encoded_text = text.encode(encoding)

    if aggressive:
        pattern = rb'(?:[\w+]+\:\/\/)?(?:[\w\d-]+\.)*[\w-]+[\.\:]\w+\/?(?:[\/\?\=\&\#\.]?[\w-]+)+\/?'
    else:
        pattern = rb'https?\:\/\/(?:[\w\d-]+\.)*[\w-]+[\.\:]\w+\/?(?:[\/\?\=\&\#\.]?[\w-]+)+\/?'

    matches = re.finditer(pattern, encoded_text)
    url_byte_positions = []
    for match in matches:
        url_bytes = match.group(0)
        url = url_bytes.decode(encoding)
        url_byte_positions.append((url, match.start(), match.end()))

    return url_byte_positions

try:
    bluesky_client = Client()
    bluesky_client.login(bluesky_account, bluesky_pwd)
    description = get_description(url)
    print(f"title: {title}")
    print(f"description: {description}")

    # LinkCardの作成
    embed_external = models.AppBskyEmbedExternal.Main(
        external=models.AppBskyEmbedExternal.External(
            title=title,
            description=description,
            uri=url
        )
    )

    text = f'{title} / {url}\n'
    
    # client_utils.TextBuilder().text(text) を使用しないで直接テキストを作成する場合はこちら
    # 投稿本文内のURLの位置を特定する
    # url_positions = extract_url_byte_positions(text, aggressive=True)
    # facets = []
    # for link in url_positions:
    #     uri = link[0] if link[0].startswith('http') else f'https://{link[0]}'
    #     facets.append(
    #         models.AppBskyRichtextFacet.Main(
    #             features=[models.AppBskyRichtextFacet.Link(uri=uri)],
    #             index=models.AppBskyRichtextFacet.ByteSlice(byte_start=link[1], byte_end=link[2]),
    #         )
    #     )
    # bluesky_client.send_post(text, embed = embed_external)

    # client_utils.TextBuilder().text(text) を使用してテキストを作成する場合はこちら
    postText = client_utils.TextBuilder().text(text)
    postText.link(title, url)
    postText.text('\n')
    for i, tag in enumerate(hash_tags):
        postText.text(f'#{tag}')
        if i < len(hash_tags) - 1:  # 最後の要素以外
            postText.text(" ")

    for i, tag in enumerate(hash_tags):
        postText.tag(f'#{tag}', f'{tag}')
        if i < len(hash_tags) - 1:  # 最後の要素以外
            postText.text(" ")

    bluesky_client.send_post(postText, embed = embed_external)
except Exception as e:
    print(f"An error occurred: {e}")