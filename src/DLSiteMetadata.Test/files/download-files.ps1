$files = @(
    "RJ246037-EN.html", "https://www.dlsite.com/maniax/work/=/product_id/RJ246037.html/?locale=en_US",
    "RJ246037-JP.html", "https://www.dlsite.com/maniax/work/=/product_id/RJ246037.html/?locale=ja_JP",
    "search-EN.html", "https://www.dlsite.com/maniax/fsr/=/language/jp/sex_category%5B0%5D/male/keyword/ONEONE1/order%5B0%5D/trend/per_page/50/from/fs.header/?locale=en_US",
    "search-JP.html", "https://www.dlsite.com/maniax/fsr/=/language/jp/sex_category%5B0%5D/male/keyword/ONEONE1/order%5B0%5D/trend/per_page/50/from/fs.header/?locale=ja_JP"
)

for ($i = 0; $i -lt $files.Count; $i+=2) {
    $file = $files[$i]
    $url = $files[$i+1]

    if ([System.IO.File]::Exists($file))
    {
        Remove-Item -Path $file
    }

    Invoke-WebRequest -Uri $url -OutFile $file
}
