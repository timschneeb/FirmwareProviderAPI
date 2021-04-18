# FirmwareProviderAPI
Unofficial distribution API for Galaxy Buds firmware binaries

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

### Download firmware
```
GET /firmware/download/R170XXU0ATD3
```
Possible error codes: `404 Not found`
