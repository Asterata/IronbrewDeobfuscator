local Byte = string.byte
local Char = string.char
local Sub = string.sub
local Concat = table.concat
local Insert = table.insert
local LDExp = math.ldexp
local GetFEnv = getfenv or function()
    return _ENV
end
local Setmetatable = setmetatable
local Select = select

local Unpack = unpack or table.unpack
local ToNumber = tonumber
local function decompress(b)
    local c, d, e = "", "", {}
    local f = 256
    local g = {}
    for h = 0, f - 1 do
        g[h] = Char(h)
    end
    local i = 1
    local function k()
        local l = ToNumber(Sub(b, i, i), 36)
        i = i + 1
        local m = ToNumber(Sub(b, i, i + l - 1), 36)
        i = i + l
        return m
    end
    c = Char(k())
    e[1] = c
    while i < #b do
        local n = k()
        if g[n] then
            d = g[n]
        else
            d = c .. Sub(c, 1, 1)
        end
        g[f] = c .. Sub(d, 1, 1)
        e[#e + 1], c, f = d, d, f + 1
    end
    return table.concat(e)
end
local ByteString =
decompress(
        "26V27227527026Z27527223Y23W24724023U27026T27924624B24224224126623T24123W24224A27727924P24X24S24Y2562732792791E25D28128227525A28628226U25A27026I27I27K27M24128J28K28L28M27026W27927P24628K27026U27924024128S28L28A27027927U27527B27D27F26Y27923U24B23X23U29129327829527C27E29427229B29D25O28229927526K29H27228A26K26J29V2722922932A228627H27528A27227429S26S29Z2A727628727928129228126K2A629Z29S2AE2A12822AH29Z27026Q2AM2AF2812AX2752A52AV27326927926K26L2AG2B42AL2AP2872AI2722AK2792712B82BF2AF2752BG2AN29Z29R2BD2AE29U2BE2BG28P2BJ2BG28626P2AV2762AU2BA27526E2722BG2AP2BE29926X2AF2C62BZ2BG2BL2BP2722992BO2BE2782BR27528P2BU2822BT29Z26W28V28126H29Z2BG26Y2CF2CW2812CZ27926D29Z29T2C52AV29X2CH27228V2A22CC2D22CF2682D62782BW2B429Y2992A92AE2DF2AZ2B7279"
)

local BitXOR = bit and bit.bxor or function(a, b)
    local p, c = 1, 0
    while a > 0 and b > 0 do
        local ra, rb = a % 2, b % 2
        if ra ~= rb then
            c = c + p
        end
        a, b, p = (a - ra) / 2, (b - rb) / 2, p * 2
    end
    if a < b then
        a = b
    end
    while a > 0 do
        local ra = a % 2
        if ra > 0 then
            c = c + p
        end
        a, p = (a - ra) / 2, p * 2
    end
    return c
end

local function gBit(Bit, Start, End)
    if End then
        local Res = (Bit / 2 ^ (Start - 1)) % 2 ^ ((End - 1) - (Start - 1) + 1)
        return Res - Res % 1
    else
        local Plc = 2 ^ (Start - 1)
        return (Bit % (Plc + Plc) >= Plc) and 1 or 0
    end
end

local Pos = 1

local function gBits32()
    local W, X, Y, Z = Byte(ByteString, Pos, Pos + 3)

    W = BitXOR(W, 254)
    X = BitXOR(X, 254)
    Y = BitXOR(Y, 254)
    Z = BitXOR(Z, 254)

    Pos = Pos + 4
    return (Z * 16777216) + (Y * 65536) + (X * 256) + W
end

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 254)
    Pos = Pos + 1
    return F
end

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2)

    W = BitXOR(W, 254)
    X = BitXOR(X, 254)

    Pos = Pos + 2
    return (X * 256) + W
end

local function gFloat()
    local Left = gBits32()
    local Right = gBits32()
    local IsNormal = 1
    local Mantissa = (gBit(Right, 1, 20) * (2 ^ 32)) + Left
    local Exponent = gBit(Right, 21, 31)
    local Sign = ((-1) ^ gBit(Right, 32))
    if (Exponent == 0) then
        if (Mantissa == 0) then
            return Sign * 0 -- +-0
        else
            Exponent = 1
            IsNormal = 0
        end
    elseif (Exponent == 2047) then
        return (Mantissa == 0) and (Sign * (1 / 0)) or (Sign * (0 / 0))
    end
    return LDExp(Sign, Exponent - 1023) * (IsNormal + (Mantissa / (2 ^ 52)))
end

local gSizet = gBits32
local function gString(Len)
    local Str
    if (not Len) then
        Len = gSizet()
        if (Len == 0) then
            return ""
        end
    end

    Str = Sub(ByteString, Pos, Pos + Len - 1)
    Pos = Pos + Len

    local FStr = {}
    for Idx = 1, #Str do
        FStr[Idx] = Char(BitXOR(Byte(Sub(Str, Idx, Idx)), 254))
    end

    return Concat(FStr)
end

local gInt = gBits32
local function _R(...)
    return {...}, Select("#", ...)
end

local function Deserialize()
    local Instrs = {}
    local Functions = {}
    local Lines = {}
    local Chunk = {
        Instrs,
        Functions,
        nil,
        Lines
    }
    local ConstCount = gBits32()
    local Consts = {}

    for Idx = 1, ConstCount do
        local Type = gBits8()
        local Cons

        if (Type == 0) then
            Cons = (gBits8() ~= 0)
        elseif (Type == 1) then
            Cons = gFloat()
        elseif (Type == 2) then
            Cons = gString()
        end

        Consts[Idx] = Cons
    end
    for Idx = 1, gBits32() do
        Functions[Idx - 1] = Deserialize()
    end
    for Idx = 1, gBits32() do
        local Descriptor = gBits8()
        if (gBit(Descriptor, 1, 1) == 0) then
            local Type = gBit(Descriptor, 2, 3)
            local Mask = gBit(Descriptor, 4, 6)

            local Inst = {
                gBits16(),
                gBits16(),
                nil,
                nil
            }

            if (Type == 0) then
                Inst[3] = gBits16()
                Inst[4] = gBits16()
            elseif (Type == 1) then
                Inst[3] = gBits32()
            elseif (Type == 2) then
                Inst[3] = gBits32() - (2 ^ 16)
            elseif (Type == 3) then
                Inst[3] = gBits32() - (2 ^ 16)
                Inst[4] = gBits16()
            end

            if (gBit(Mask, 1, 1) == 1) then
                Inst[2] = Consts[Inst[2]]
            end
            if (gBit(Mask, 2, 2) == 1) then
                Inst[3] = Consts[Inst[3]]
            end
            if (gBit(Mask, 3, 3) == 1) then
                Inst[4] = Consts[Inst[4]]
            end

            Instrs[Idx] = Inst
        end
    end
    Chunk[3] = gBits8()
    return Chunk
end
local function Wrap(Chunk, Upvalues, Env)
    local Instr = Chunk[1]
    local Proto = Chunk[2]
    local Params = Chunk[3]

    return function(...)
        local Instr = Instr
        local Proto = Proto
        local Params = Params

        local _R = _R
        local InstrPoint = 1
        local Top = -1

        local Vararg = {}
        local Args = {...}

        local PCount = Select("#", ...) - 1

        local Lupvals = {}
        local Stk = {}

        for Idx = 0, PCount do
            if (Idx >= Params) then
                Vararg[Idx - Params] = Args[Idx + 1]
            else
                Stk[Idx] = Args[Idx + 1]
            end
        end

        local Varargsz = PCount - Params + 1

        local Inst
        local Enum

        while true do
            Inst = Instr[InstrPoint]
            Enum = Inst[1]
            if Enum <= 12 then
                if Enum <= 5 then
                    if Enum <= 2 then
                        if Enum <= 0 then
                            Stk[Inst[2]]()
                        elseif Enum > 1 then
                            local A = Inst[2]
                            Stk[A](Stk[A + 1])
                        else
                            Stk[Inst[2]] = Wrap(Proto[Inst[3]], nil, Env)
                        end
                    elseif Enum <= 3 then
                        InstrPoint = Inst[3]
                    elseif Enum == 4 then
                        Stk[Inst[2]] = Stk[Inst[3]]
                    else
                        Stk[Inst[2]] = Env[Inst[3]]
                    end
                elseif Enum <= 8 then
                    if Enum <= 6 then
                        Stk[Inst[2]]()
                    elseif Enum == 7 then
                        Stk[Inst[2]] = Env[Inst[3]]
                    else
                        if (Stk[Inst[2]] ~= Stk[Inst[4]]) then
                            InstrPoint = InstrPoint + 1
                        else
                            InstrPoint = Inst[3]
                        end
                    end
                elseif Enum <= 10 then
                    if Enum > 9 then
                        local A
                        Stk[Inst[2]] = Env[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Stk[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Env[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Inst[3]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
                    else
                        Stk[Inst[2]] = {}
                    end
                elseif Enum > 11 then
                    Stk[Inst[2]] = Wrap(Proto[Inst[3]], nil, Env)
                else
                    do
                        return
                    end
                end
            elseif Enum <= 19 then
                if Enum <= 15 then
                    if Enum <= 13 then
                        Stk[Inst[2]] = {}
                    elseif Enum > 14 then
                        Stk[Inst[2]] = Stk[Inst[3]]
                    else
                        local A = Inst[2]
                        local T = Stk[A]
                        for Idx = A + 1, Inst[3] do
                            Insert(T, Stk[Idx])
                        end
                    end
                elseif Enum <= 17 then
                    if Enum > 16 then
                        if (Stk[Inst[2]] ~= Stk[Inst[4]]) then
                            InstrPoint = InstrPoint + 1
                        else
                            InstrPoint = Inst[3]
                        end
                    else
                        local A = Inst[2]
                        local T = Stk[A]
                        for Idx = A + 1, Inst[3] do
                            Insert(T, Stk[Idx])
                        end
                    end
                elseif Enum > 18 then
                    local T
                    local A
                    Stk[Inst[2]] = Env[Inst[3]]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    A = Inst[2]
                    Stk[A](Stk[A + 1])
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Env[Inst[3]] = Stk[Inst[2]]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = {}
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    A = Inst[2]
                    T = Stk[A]
                    for Idx = A + 1, Inst[3] do
                        Insert(T, Stk[Idx])
                    end
                else
                    do
                        return
                    end
                end
            elseif Enum <= 22 then
                if Enum <= 20 then
                    Stk[Inst[2]] = Inst[3]
                elseif Enum > 21 then
                    Env[Inst[3]] = Stk[Inst[2]]
                else
                    Stk[Inst[2]] = Inst[3]
                end
            elseif Enum <= 24 then
                if Enum == 23 then
                    InstrPoint = Inst[3]
                else
                    local T
                    local A
                    Stk[Inst[2]] = Stk[Inst[3]]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    A = Inst[2]
                    Stk[A](Stk[A + 1])
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = {}
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    A = Inst[2]
                    T = Stk[A]
                    for Idx = A + 1, Inst[3] do
                        Insert(T, Stk[Idx])
                    end
                end
            elseif Enum == 25 then
                local A = Inst[2]
                Stk[A](Stk[A + 1])
            else
                Env[Inst[3]] = Stk[Inst[2]]
            end
            InstrPoint = InstrPoint + 1
        end
    end
end
return Wrap(Deserialize(), {}, GetFEnv())()
