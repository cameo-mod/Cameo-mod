# Cameo-mod dedicated server systemd units

The dedicated game servers ("Cameo's Domain") run as **two templated systemd
units**, instantiated once per listen port. Each instance runs
`/etc/Cameo-mod/launch-dedicated.sh` (the stock OpenRA SDK launcher, which loops
internally) as the `cameo` user from `/etc/Cameo-mod`.

- **`cameo-dedicated@.service`** — regular open server.
- **`cameo-dedicated-ranked@.service`** — login-gated: `RequireAuthentication`
  + `RecordReplays` on (for the 1v1 ladder).

The systemd **instance name is the listen port** (`%i`), so
`cameo-dedicated@11200` listens on 11200. The only per-realm difference is the
advertised display name, which lives in `/etc/cameo-realms/<port>.env`
(mirrored here under [`realms/`](realms/)).

| Instance                          | Port  | Display name                          |
|-----------------------------------|-------|---------------------------------------|
| `cameo-dedicated@11200`           | 11200 | Cameo's Domain -- 1st Realm           |
| `cameo-dedicated@11201`           | 11201 | Cameo's Domain -- 2nd Realm           |
| `cameo-dedicated-ranked@11202`    | 11202 | Cameo's Domain -- 3rd Realm [RANKED 1v1] |
| `cameo-dedicated-ranked@11203`    | 11203 | Cameo's Domain -- 4th Realm [RANKED 1v1] |

The realm env files live in `/etc/cameo-realms/` — **outside** the
`/etc/Cameo-mod` git checkout — so the `cameo-update` deploy (which does
`rsync --delete` into the checkout) never touches them. No passwords or secrets
are stored in any unit or env file; ranked auth uses the stock
`forum.openra.net` player database.

## Install / update

```sh
# Unit templates
sudo cp cameo-dedicated@.service cameo-dedicated-ranked@.service /etc/systemd/system/

# Per-realm display names
sudo mkdir -p /etc/cameo-realms
sudo cp realms/*.env /etc/cameo-realms/

sudo systemctl daemon-reload
sudo systemctl enable --now cameo-dedicated@11200.service cameo-dedicated@11201.service
sudo systemctl enable --now cameo-dedicated-ranked@11202.service cameo-dedicated-ranked@11203.service
```

To add another open realm, drop a `realms/<port>.env` and
`systemctl enable --now cameo-dedicated@<port>.service` — no new unit file.

The periodic auto-updater that redeploys these from the latest `playtest-*`
tag lives in [`../cameo-update/`](../cameo-update/). Its `SERVICES` list still
names the concrete instances and must be kept in sync with the enabled realms.
