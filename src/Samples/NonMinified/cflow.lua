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
        "1Q1H2751I132751H22G22B22622K22822122622A22F22L22M21Y22621U21X22821V21X1I1K27922P22R23C23J22T1I27427523D23823H23H23I21D22U23I22R23H23927T27921Y22M21V22L22D27928M26921A28M28M22928Q2751P2291I1527928428623I29229329429523I1I1N27928A23D2931I1P27923J23I29C2941I1127927B27D27F27H27J27L22622C22N28L1G2791J27928F27527W27Y2801L27922T23822Q22T29Y2791M27913101H29Y2AD28M2A028T2AJ1H2A029Y2782AP2AL2AT2A02AN28M29Y2AR28T2AL24X28M1I2AO1J1S2AO1H2B92AZ1H131429Z2AG29M29Y2B6279122BH2AS29M2752A727513162792BS28U2A12BI2AP27U2BT29M2A02992C31H2B62AF2BW2BZ2AD2B92AL2751J1R2AQ28T2B92B62AY1329M2A729F2BM2C82CJ2AD29F2B62BL2BE2CP1H2C22CZ2D11H2C62D32992CA2BR2CT29929Y1N2AK1H23W2AP2B61L182792DH29Y2DK27923Z2AP2BF2CT2CN2D01O28M2BN2CM2AP2DP27523Y2DS2BG2E02BT2742A72CI28Q2E02B02BA2AO2CF2752AL1N2DC1H21I2DI1H2E22EM2E12DL27521O2ES2791Z2EW2752AI2DD2DF2122EO2EQ2F42DO2ET1H2192EZ1H1B2AP2EK2AP1X2F52F92FI2F827928S2FM27529X2EP2F92CI2DD2EL2102FJ2792FX2FP27A2FC22N2FC21L2FF2EL2172FY2752G92G121V2FC21Z2FC1T2FF2DF2232GA1H2GL2G12262FC21P2GJ2AP2392GM2GV2G123D2GH2G72AP22E2GM2H32G123E2FC23I2FC1W2H129Y22L2GM2HE2G122R2FC22V2FC2B62F22AP22Z2GM2HP2G12302FC2132GT29Y2412GM2HY2G12452FC2B12DE2AP2462GM2I72G124A2FC2162HC1H2242GM2IG2G124E2FC23M2HL2IE2372GM2IP2G123Q2FC23V2FC28P2FV2AP23A2GM2J02G11F2FC2BJ2D52DF2J62DJ2F92J62EQ22A2IE23J2GM2JG2G12DF2EQ1V2HW1H2AF2BK2FS2AE2FC22H2IE22S2GM2JW2G127U2G12A72IY29Y22W2GM2K52G129F2G122K2JN2DF2JA2792JK2FT2JN2EL2KE2752EL2EQ21S2IE2422GM2KQ2JJ2I32JN2JP2CT2EQ2KW2EQ21W2IE24B2GM2L32G12JC2KH2K31H24F2GM2LB2G128Y2G12202IE23N2GM2LJ2JZ2FC2F12D52EL23R2GM2LR2LE2FC2272JN2JM2JQ2JL2J52JN2742LZ2F92M32JR2752362FC21H2FC21K2FC21C2AP"
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

    W = BitXOR(W, 17)
    X = BitXOR(X, 17)
    Y = BitXOR(Y, 17)
    Z = BitXOR(Z, 17)

    Pos = Pos + 4
    return (Z * 16777216) + (Y * 65536) + (X * 256) + W
end

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 17)
    Pos = Pos + 1
    return F
end

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2)

    W = BitXOR(W, 17)
    X = BitXOR(X, 17)

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
        FStr[Idx] = Char(BitXOR(Byte(Sub(Str, Idx, Idx)), 17))
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

        if (Type == 2) then
            Cons = (gBits8() ~= 0)
        elseif (Type == 0) then
            Cons = gFloat()
        elseif (Type == 3) then
            Cons = gString()
        end

        Consts[Idx] = Cons
    end
    Chunk[3] = gBits8()
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
            if Enum <= 13 then
                if Enum <= 6 then
                    if Enum <= 2 then
                        if Enum <= 0 then
                            Stk = {}
                            for Idx = 0, PCount do
                                if Idx < Params then
                                    Stk[Idx] = Args[Idx + 1]
                                else
                                    break
                                end
                            end
                        elseif Enum == 1 then
                            if (Stk[Inst[2]] == Stk[Inst[4]]) then
                                InstrPoint = InstrPoint + 1
                            else
                                InstrPoint = Inst[3]
                            end
                        else
                            Stk[Inst[2]] = Wrap(Proto[Inst[3]], nil, Env)
                        end
                    elseif Enum <= 4 then
                        if Enum > 3 then
                            local A = Inst[2]
                            local T = Stk[A]
                            for Idx = A + 1, Inst[3] do
                                Insert(T, Stk[Idx])
                            end
                        else
                            Stk = {}
                            for Idx = 0, PCount do
                                if Idx < Params then
                                    Stk[Idx] = Args[Idx + 1]
                                else
                                    break
                                end
                            end
                        end
                    elseif Enum == 5 then
                        if (Stk[Inst[2]] == Stk[Inst[4]]) then
                            InstrPoint = InstrPoint + 1
                        else
                            InstrPoint = Inst[3]
                        end
                    else
                        if (Stk[Inst[2]] ~= Stk[Inst[4]]) then
                            InstrPoint = InstrPoint + 1
                        else
                            InstrPoint = Inst[3]
                        end
                    end
                elseif Enum <= 9 then
                    if Enum <= 7 then
                        Stk[Inst[2]] = Stk[Inst[3]]
                    elseif Enum == 8 then
                        Stk[Inst[2]] = {}
                    else
                        do
                            return
                        end
                    end
                elseif Enum <= 11 then
                    if Enum == 10 then
                        Stk[Inst[2]] = Wrap(Proto[Inst[3]], nil, Env)
                    else
                        local A
                        Stk[Inst[2]] = Inst[3]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Stk[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Stk[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        do
                            return
                        end
                    end
                elseif Enum == 12 then
                    Stk[Inst[2]] = Env[Inst[3]]
                else
                    Stk[Inst[2]] = Stk[Inst[3]]
                end
            elseif Enum <= 20 then
                if Enum <= 16 then
                    if Enum <= 14 then
                        Stk[Inst[2]] = {}
                    elseif Enum == 15 then
                        local A = Inst[2]
                        Stk[A](Stk[A + 1])
                    else
                        Stk[Inst[2]] = Inst[3]
                    end
                elseif Enum <= 18 then
                    if Enum == 17 then
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
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        do
                            return
                        end
                    else
                        InstrPoint = Inst[3]
                    end
                elseif Enum > 19 then
                    Env[Inst[3]] = Stk[Inst[2]]
                else
                    local A = Inst[2]
                    Stk[A](Stk[A + 1])
                end
            elseif Enum <= 23 then
                if Enum <= 21 then
                    Stk[Inst[2]] = Env[Inst[3]]
                elseif Enum == 22 then
                    if (Stk[Inst[2]] ~= Stk[Inst[4]]) then
                        InstrPoint = InstrPoint + 1
                    else
                        InstrPoint = Inst[3]
                    end
                else
                    Env[Inst[3]] = Stk[Inst[2]]
                end
            elseif Enum <= 25 then
                if Enum == 24 then
                    do
                        return
                    end
                else
                    InstrPoint = Inst[3]
                end
            elseif Enum == 26 then
                local A = Inst[2]
                local T = Stk[A]
                for Idx = A + 1, Inst[3] do
                    Insert(T, Stk[Idx])
                end
            else
                Stk[Inst[2]] = Inst[3]
            end
            InstrPoint = InstrPoint + 1
        end
    end
end
return Wrap(Deserialize(), {}, GetFEnv())()
