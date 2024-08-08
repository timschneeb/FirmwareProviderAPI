# FirmwareProviderAPI
Unofficial distribution API for Galaxy Buds firmware binaries with built-in FW scraping capabilities

I'm currently hosting an API instance here. I do not provide any uptime guarantee for this server.
```
https://fw.timschneeberger.me/v3/{endpoint}
```

## API reference

### List all available firmware including metadata
```
GET /firmware
```
```
[
  {
    "model": "Buds",
    "buildName": "R170XXU0ATD3",
    "region": "XX",
    "bootloaderVersion": "U0",
    "reservedField": "A",
    "year": 2020,
    "month": 4,
    "revision": 3
  }
]
```

### List all available firmware for a certain model
```
GET /firmware/{model}
```
```
[
  {
    "model": "Buds",
    "buildName": "R170XXU0ATD3",
    "region": "XX",
    "bootloaderVersion": "U0",
    "reservedField": "A",
    "year": 2020,
    "month": 4,
    "revision": 3
  }
]
```
`{model}` represents either `Buds`, `BudsPlus`, `BudsLive`, `BudsPro`, `Buds2`, `Buds2Pro`, `BudsFE`, `Buds3` or `Buds3Pro`.

### Download firmware
```
GET /firmware/download/R170XXU0ATD3
```
Possible error codes: `404 Not found`
