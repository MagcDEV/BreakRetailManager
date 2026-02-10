# ARCA WSFE – Single-File LLM-Ready Spec to Obtain CAE & Expiration

> Purpose: Everything you need in one place to request the **CAE** and its **expiration date (CAEFchVto)** for retail invoicing via ARCA WSFE. Designed for LLM prompts and engineering handoff.

---

## 1) Top-Level Flow

1. Authenticate with **WSAA** to obtain `Token` and `Sign` using `service="wsfe"` (ticket validity ≈ 12h; cert bound to “Facturación Electrónica”). citeturn1search1
2. Call **`FECAESolicitar`** with minimal invoice data (Auth + header + detail). citeturn1search1
3. Read **`CAE`** and **`CAEFchVto`** at:
   `FECAESolicitarResponse/FECAESolicitarResult/FeDetResp/FEDetResponse/{CAE, CAEFchVto}`. citeturn1search1
4. On timeout/unknown status, **do not resubmit blindly**—query **`FECompConsultar`** (or **`FECompUltimoAutorizado`** to learn the last number) to avoid correlativity errors. citeturn1search1

---

## 2) Endpoints & Authentication

- **Homologation (testing) SOAP base:** `https://wswhomo.afip.gov.ar/wsfev1/service.asmx` (WSDL `...?WSDL`). citeturn1search1
- **Production SOAP base:** `https://servicios1.afip.gov.ar/wsfev1/service.asmx` (WSDL `...?WSDL`). citeturn1search1
- **WSAA:** Obtain `Token` & `Sign` using `service="wsfe"`; **ticket validity ~ 12h**; cert associated to **“Facturación Electrónica”**. citeturn1search1

---

## 3) Method to Obtain CAE

### 3.1 `FECAESolicitar` — Request (Minimum)

SOAP Action (Homologation example): `.../service.asmx?op=FECAESolicitar`. citeturn1search1

**Structure (essentials only)**
```xml
<FECAESolicitar xmlns="http://ar.gov.afip.dif.FEV1/">
  <Auth>
    <Token>string</Token>            <!-- from WSAA -->
    <Sign>string</Sign>              <!-- from WSAA -->
    <Cuit>long</Cuit>                <!-- issuer CUIT -->
  </Auth>
  <FeCAEReq>
    <FeCabReq>
      <CantReg>int</CantReg>         <!-- usually 1 -->
      <PtoVta>int</PtoVta>
      <CbteTipo>int</CbteTipo>
    </FeCabReq>
    <FeDetReq>
      <FECAEDetRequest>
        <Concepto>int</Concepto>     <!-- 1=Productos, 2=Servicios, 3=Prod+Serv -->
        <DocTipo>int</DocTipo>
        <DocNro>long</DocNro>
        <CbteDesde>long</CbteDesde>
        <CbteHasta>long</CbteHasta>
        <CbteFch>string</CbteFch>    <!-- yyyymmdd -->
        <ImpTotal>double</ImpTotal>
        <ImpTotConc>double</ImpTotConc>
        <ImpNeto>double</ImpNeto>
        <ImpOpEx>double</ImpOpEx>
        <ImpTrib>double</ImpTrib>
        <ImpIVA>double</ImpIVA>
        <MonId>string</MonId>        <!-- e.g., PES -->
        <MonCotiz>double</MonCotiz>  <!-- 1 for PES -->
        <!-- Optional arrays: Tributos, Iva, Opcionales, Compradores, PeriodoAsoc, Actividades -->
        <!-- Optional as of v4.0+: CanMisMonExt (S/N), CondicionIVAReceptorId (mandatory per RG 5616 when enforced) -->
      </FECAEDetRequest>
    </FeDetReq>
  </FeCAEReq>
</FECAESolicitar>
```
Notes & formats: dates `yyyymmdd`; `FchProceso` headers use `yyyymmddhhmiss`. For `MonId=PES`, set `MonCotiz=1`. citeturn1search1

**Minimal field list** (Auth + FeCabReq + FECAEDetRequest) is explicitly defined in the manual. citeturn1search2

### 3.2 `FECAESolicitar` — Response (Where CAE lives)

**Path to CAE & expiration date**
```
FECAESolicitarResponse
  └─ FECAESolicitarResult
     ├─ FeCabResp/Resultado  (A=aprobado, R=rechazado, P=parcial)
     └─ FeDetResp/FEDetResponse
          ├─ CAE         <-- authorization code
          ├─ CAEFchVto   <-- CAE expiration (yyyymmdd)
          └─ (Obs/Errors if any)
```
citeturn1search1

**Example (abridged)**
```xml
<FECAESolicitarResponse>
  <FECAESolicitarResult>
    <FeCabResp>
      <Resultado>A</Resultado>
    </FeCabResp>
    <FeDetResp>
      <FEDetResponse>
        <Resultado>A</Resultado>
        <CAE>41124578989845</CAE>
        <CAEFchVto>20100913</CAEFchVto>
      </FEDetResponse>
    </FeDetResp>
  </FECAESolicitarResult>
</FECAESolicitarResponse>
```
citeturn1search1

---

## 4) Idempotency & Fallback (Timeouts / Unknown Status)

- If the app times out after `FECAESolicitar`, use **`FECompConsultar`** (by `CbteTipo`, `PtoVta`, `CbteNro`) to retrieve the invoice (including **`CAE`** and **`CAEFchVto`**). citeturn1search1
- Use **`FECompUltimoAutorizado`** to get the last authorized number for (`PtoVta`, `CbteTipo`) and maintain correlativity. citeturn1search1

> This avoids sequence/date issues (e.g., error **10016**). citeturn1search1

---

## 5) Data Formats & Constraints to Enforce

- **Dates**: `CbteFch`, `FchServDesde`, `FchServHasta`, `FchVtoPago`, `CAEFchVto` ⇒ `yyyymmdd`; `FchProceso` ⇒ `yyyymmddhhmiss`. citeturn1search1
- **Currency**: `MonId` required; if `MonId=PES` then `MonCotiz=1`. If using **same-currency foreign collection (`CanMisMonExt=S`)**, apply strict/exact `MonCotiz` rule per v4.0 change. citeturn1search1
- **Concept values**: `1=Productos`, `2=Servicios`, `3=Productos y Servicios`. citeturn1search1
- **Result codes**: `A`, `R`, `P` in `FeCabResp/Resultado`. citeturn1search1

---

## 6) Common Validation Failures That Block CAE

- **Auth/CUIT & enrollment**: 10000 (registrations/authorization/domicilio), 600–602 (token/CUIT mismatches). citeturn1search1
- **Header basics**: 10001–10003 (counts), 10005 (`PtoVta` must be a RECE point), 10007 (valid `CbteTipo`). citeturn1search1
- **Correlativity & dates**: **10016** (next number & date windows per concept and month rule for Products). citeturn1search1
- **Currency**: 10037 (MonId), **10039** (MonCotiz=1 for PES), **10038** (cotización rules or omission when `CanMisMonExt=S` applies). citeturn1search1
- **Totals**: 10048 (`ImpTotal` = sum of components with tight tolerance). citeturn1search1

---

## 7) LLM Extraction Map (Drop-in YAML)

```yaml
arca_wsfe:
  auth:
    wsaa_service: "wsfe"
    token_validity_hours: 12
  endpoints:
    homologation: "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"
    production:   "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
  method_get_cae: FECAESolicitar
  request_minimum:
    Auth: [Token, Sign, Cuit]
    FeCabReq: [CantReg, PtoVta, CbteTipo]
    FECAEDetRequest_required:
      - Concepto        # 1,2,3
      - DocTipo         # int
      - DocNro          # long
      - CbteDesde       # long
      - CbteHasta       # long
      - CbteFch         # yyyymmdd
      - ImpTotal        # double
      - ImpTotConc      # double
      - ImpNeto         # double
      - ImpOpEx         # double
      - ImpTrib         # double
      - ImpIVA          # double
      - MonId           # e.g. PES
      - MonCotiz        # 1 if MonId=PES
  response_extract:
    cae_xpath: "/FECAESolicitarResponse/FECAESolicitarResult/FeDetResp/FEDetResponse/CAE"
    cae_expiration_xpath: "/FECAESolicitarResponse/FECAESolicitarResult/FeDetResp/FEDetResponse/CAEFchVto"  # yyyymmdd
    result_xpath: "/FECAESolicitarResponse/FECAESolicitarResult/FeCabResp/Resultado"  # A|R|P
  fallback_on_timeout:
    first_try: FECompConsultar
    second_try: FECompUltimoAutorizado
  date_formats:
    invoice_date: "yyyymmdd"
    process_stamp: "yyyymmddhhmiss"
```
All items mirror manual definitions and locations. citeturn1search1turn1search2

---

## 8) Minimal Working Example (Short)

**Request (1 invoice; essentials):** citeturn1search1
```xml
<FECAESolicitar xmlns="http://ar.gov.afip.dif.FEV1/">
  <Auth>
    <Token>...</Token>
    <Sign>...</Sign>
    <Cuit>33693450239</Cuit>
  </Auth>
  <FeCAEReq>
    <FeCabReq>
      <CantReg>1</CantReg>
      <PtoVta>12</PtoVta>
      <CbteTipo>1</CbteTipo>
    </FeCabReq>
    <FeDetReq>
      <FECAEDetRequest>
        <Concepto>1</Concepto>
        <DocTipo>80</DocTipo>
        <DocNro>20111111112</DocNro>
        <CbteDesde>1</CbteDesde>
        <CbteHasta>1</CbteHasta>
        <CbteFch>20100903</CbteFch>
        <ImpTotal>184.05</ImpTotal>
        <ImpTotConc>0</ImpTotConc>
        <ImpNeto>150</ImpNeto>
        <ImpOpEx>0</ImpOpEx>
        <ImpTrib>7.8</ImpTrib>
        <ImpIVA>26.25</ImpIVA>
        <MonId>PES</MonId>
        <MonCotiz>1</MonCotiz>
      </FECAEDetRequest>
    </FeDetReq>
  </FeCAEReq>
</FECAESolicitar>
```
**Response (CAE fragment):** citeturn1search1
```xml
<FECAESolicitarResponse>
  <FECAESolicitarResult>
    <FeCabResp>
      <Resultado>A</Resultado>
    </FeCabResp>
    <FeDetResp>
      <FEDetResponse>
        <Resultado>A</Resultado>
        <CAE>41124578989845</CAE>
        <CAEFchVto>20100913</CAEFchVto>
      </FEDetResponse>
    </FeDetResp>
  </FECAESolicitarResult>
</FECAESolicitarResponse>
```

---

## 9) Quick Pre-Checks to Avoid Rejection

- `PtoVta` must be enabled RECE point; use `FEParamGetPtosVenta` to list; error 10005 if not. citeturn1search1
- Predict next number and respect date windows; otherwise 10016. `FECompUltimoAutorizado` helps. citeturn1search1
- `MonId` and `MonCotiz` rules (PES ⇒ 1). 10037, 10039, 10038. citeturn1search1
- `ImpTotal` equals the sum of parts (10048; strict tolerance). citeturn1search1

---

## 10) LLM Helper Prompt (for your agents)

> You are integrating ARCA WSFE to obtain CAE and `CAEFchVto`. Use the **`arca_wsfe`** YAML above as ground truth. Build `FECAESolicitar` requests with required fields and formats (dates `yyyymmdd`, `MonCotiz=1` if `PES`). After calling, parse `FeCabResp/Resultado`; on `A`, extract `FeDetResp/FEDetResponse/CAE` and `CAEFchVto`. On timeout/unknown status, call `FECompConsultar` with (`CbteTipo`, `PtoVta`, `CbteNro`) to retrieve the same; otherwise query `FECompUltimoAutorizado` for correlativity. Pre-validate errors 10005, 10016, 10037–10039, 10048 and emit helpful messages.

(Backed entirely by ARCA WSFE v4.1 manual excerpts.) citeturn1search1turn1search2

