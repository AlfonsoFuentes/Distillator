using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.PropertiesDtos.WaterProperties
{
    using System;

    public static class CPropiAgua
    {
        public const double rgas_water = 461.526;    // gas constant in J/(kg K)
        public const double tc_water = 647.096;      // critical temperature in K
        public const double pc_water = 220.64;       // critical pressure in bar
        public const double dc_water = 322.0;        // critical density in kg/m**3

        // Arreglos de Coeficientes
        private static readonly int[] ireg1 = new int[35];
        private static readonly int[] jreg1 = new int[35];
        private static readonly double[] nreg1 = new double[35];

        private static readonly int[] j0reg2 = new int[10];
        private static readonly double[] n0reg2 = new double[10];
        private static readonly int[] ireg2 = new int[44];
        private static readonly int[] jreg2 = new int[44];
        private static readonly double[] nreg2 = new double[44];

        private static readonly int[] ireg3 = new int[41];
        private static readonly int[] jreg3 = new int[41];
        private static readonly double[] nreg3 = new double[41];

        private static readonly int[] ivisc = new int[20];
        private static readonly int[] jvisc = new int[20];
        private static readonly double[] nreg4 = new double[11];
        private static readonly double[] nbound = new double[6];
        private static readonly double[] n0visc = new double[4];
        private static readonly double[] nvisc = new double[20];
        private static readonly double[] n0thcon = new double[4];
        private static readonly double[,] nthcon = new double[5, 6];

        // El constructor estático se ejecuta solo una vez en la vida de la aplicación
        static CPropiAgua()
        {
            InitFieldsreg1();
            InitFieldsreg2();
            InitFieldsreg3();
            InitFieldsreg4();
            InitFieldsbound();
            InitFieldsvisc();
            InitFieldsthcon();
        }

        #region Inicialización de Campos (Traducción Exacta)

        private static void InitFieldsreg1()
        {
            ireg1[1] = 0; ireg1[2] = 0; ireg1[3] = 0; ireg1[4] = 0; ireg1[5] = 0; ireg1[6] = 0; ireg1[7] = 0; ireg1[8] = 0;
            ireg1[9] = 1; ireg1[10] = 1; ireg1[11] = 1; ireg1[12] = 1; ireg1[13] = 1; ireg1[14] = 1;
            ireg1[15] = 2; ireg1[16] = 2; ireg1[17] = 2; ireg1[18] = 2; ireg1[19] = 2;
            ireg1[20] = 3; ireg1[21] = 3; ireg1[22] = 3;
            ireg1[23] = 4; ireg1[24] = 4; ireg1[25] = 4;
            ireg1[26] = 5; ireg1[27] = 8; ireg1[28] = 8;
            ireg1[29] = 21; ireg1[30] = 23; ireg1[31] = 29; ireg1[32] = 30; ireg1[33] = 31; ireg1[34] = 32;

            jreg1[1] = -2; jreg1[2] = -1; jreg1[3] = 0; jreg1[4] = 1; jreg1[5] = 2; jreg1[6] = 3; jreg1[7] = 4; jreg1[8] = 5;
            jreg1[9] = -9; jreg1[10] = -7; jreg1[11] = -1; jreg1[12] = 0; jreg1[13] = 1; jreg1[14] = 3;
            jreg1[15] = -3; jreg1[16] = 0; jreg1[17] = 1; jreg1[18] = 3; jreg1[19] = 17;
            jreg1[20] = -4; jreg1[21] = 0; jreg1[22] = 6;
            jreg1[23] = -5; jreg1[24] = -2; jreg1[25] = 10;
            jreg1[26] = -8; jreg1[27] = -11; jreg1[28] = -6;
            jreg1[29] = -29; jreg1[30] = -31; jreg1[31] = -38; jreg1[32] = -39; jreg1[33] = -40; jreg1[34] = -41;

            nreg1[1] = 0.14632971213167; nreg1[2] = -0.84548187169114; nreg1[3] = -3.756360367204; nreg1[4] = 3.3855169168385;
            nreg1[5] = -0.95791963387872; nreg1[6] = 0.15772038513228; nreg1[7] = -0.016616417199501; nreg1[8] = 8.1214629983568E-04;
            nreg1[9] = 2.8319080123804E-04; nreg1[10] = -6.0706301565874E-04; nreg1[11] = -0.018990068218419; nreg1[12] = -0.032529748770505;
            nreg1[13] = -0.021841717175414; nreg1[14] = -5.283835796993E-05; nreg1[15] = -4.7184321073267E-04; nreg1[16] = -3.0001780793026E-04;
            nreg1[17] = 4.7661393906987E-05; nreg1[18] = -4.4141845330846E-06; nreg1[19] = -7.2694996297594E-16; nreg1[20] = -3.1679644845054E-05;
            nreg1[21] = -2.8270797985312E-06; nreg1[22] = -8.5205128120103E-10; nreg1[23] = -2.2425281908E-06; nreg1[24] = -6.5171222895601E-07;
            nreg1[25] = -1.4341729937924E-13; nreg1[26] = -4.0516996860117E-07; nreg1[27] = -1.2734301741641E-09; nreg1[28] = -1.7424871230634E-10;
            nreg1[29] = -6.8762131295531E-19; nreg1[30] = 1.4478307828521E-20; nreg1[31] = 2.6335781662795E-23; nreg1[32] = -1.1947622640071E-23;
            nreg1[33] = 1.8228094581404E-24; nreg1[34] = -9.3537087292458E-26;
        }

        private static void InitFieldsreg2()
        {
            j0reg2[1] = 0; j0reg2[2] = 1; j0reg2[3] = -5; j0reg2[4] = -4; j0reg2[5] = -3; j0reg2[6] = -2; j0reg2[7] = -1; j0reg2[8] = 2; j0reg2[9] = 3;
            n0reg2[1] = -9.6927686500217; n0reg2[2] = 10.086655968018; n0reg2[3] = -0.005608791128302; n0reg2[4] = 0.071452738081455;
            n0reg2[5] = -0.40710498223928; n0reg2[6] = 1.4240819171444; n0reg2[7] = -4.383951131945; n0reg2[8] = -0.28408632460772; n0reg2[9] = 0.021268463753307;

            ireg2[1] = 1; ireg2[2] = 1; ireg2[3] = 1; ireg2[4] = 1; ireg2[5] = 1; ireg2[6] = 2; ireg2[7] = 2; ireg2[8] = 2; ireg2[9] = 2; ireg2[10] = 2;
            ireg2[11] = 3; ireg2[12] = 3; ireg2[13] = 3; ireg2[14] = 3; ireg2[15] = 3; ireg2[16] = 4; ireg2[17] = 4; ireg2[18] = 4; ireg2[19] = 5;
            ireg2[20] = 6; ireg2[21] = 6; ireg2[22] = 6; ireg2[23] = 7; ireg2[24] = 7; ireg2[25] = 7; ireg2[26] = 8; ireg2[27] = 8; ireg2[28] = 9;
            ireg2[29] = 10; ireg2[30] = 10; ireg2[31] = 10; ireg2[32] = 16; ireg2[33] = 16; ireg2[34] = 18; ireg2[35] = 20; ireg2[36] = 20;
            ireg2[37] = 20; ireg2[38] = 21; ireg2[39] = 22; ireg2[40] = 23; ireg2[41] = 24; ireg2[42] = 24; ireg2[43] = 24;

            jreg2[1] = 0; jreg2[2] = 1; jreg2[3] = 2; jreg2[4] = 3; jreg2[5] = 6; jreg2[6] = 1; jreg2[7] = 2; jreg2[8] = 4; jreg2[9] = 7; jreg2[10] = 36;
            jreg2[11] = 0; jreg2[12] = 1; jreg2[13] = 3; jreg2[14] = 6; jreg2[15] = 35; jreg2[16] = 1; jreg2[17] = 2; jreg2[18] = 3; jreg2[19] = 7;
            jreg2[20] = 3; jreg2[21] = 16; jreg2[22] = 35; jreg2[23] = 0; jreg2[24] = 11; jreg2[25] = 25; jreg2[26] = 8; jreg2[27] = 36; jreg2[28] = 13;
            jreg2[29] = 4; jreg2[30] = 10; jreg2[31] = 14; jreg2[32] = 29; jreg2[33] = 50; jreg2[34] = 57; jreg2[35] = 20; jreg2[36] = 35;
            jreg2[37] = 48; jreg2[38] = 21; jreg2[39] = 53; jreg2[40] = 39; jreg2[41] = 26; jreg2[42] = 40; jreg2[43] = 58;

            nreg2[1] = -1.7731742473213E-03; nreg2[2] = -0.017834862292358; nreg2[3] = -0.045996013696365; nreg2[4] = -0.057581259083432; nreg2[5] = -0.05032527872793;
            nreg2[6] = -3.3032641670203E-05; nreg2[7] = -1.8948987516315E-04; nreg2[8] = -3.9392777243355E-03; nreg2[9] = -0.043797295650573; nreg2[10] = -2.6674547914087E-05;
            nreg2[11] = 2.0481737692309E-08; nreg2[12] = 4.3870667284435E-07; nreg2[13] = -3.227767723857E-05; nreg2[14] = -1.5033924542148E-03; nreg2[15] = -0.040668253562649;
            nreg2[16] = -7.8847309559367E-10; nreg2[17] = 1.2790717852285E-08; nreg2[18] = 4.8225372718507E-07; nreg2[19] = 2.2922076337661E-06; nreg2[20] = -1.6714766451061E-11;
            nreg2[21] = -2.1171472321355E-03; nreg2[22] = -23.895741934104; nreg2[23] = -5.905956432427E-18; nreg2[24] = -1.2621808899101E-06; nreg2[25] = -0.038946842435739;
            nreg2[26] = 1.1256211360459E-11; nreg2[27] = -8.2311340897998; nreg2[28] = 1.9809712802088E-08; nreg2[29] = 1.0406965210174E-19; nreg2[30] = -1.0234747095929E-13;
            nreg2[31] = -1.0018179379511E-09; nreg2[32] = -8.0882908646985E-11; nreg2[33] = 0.10693031879409; nreg2[34] = -0.33662250574171; nreg2[35] = 8.9185845355421E-25;
            nreg2[36] = 3.0629316876232E-13; nreg2[37] = -4.2002467698208E-06; nreg2[38] = -5.9056029685639E-26; nreg2[39] = 3.7826947613457E-06; nreg2[40] = -1.2768608934681E-15;
            nreg2[41] = 7.3087610595061E-29; nreg2[42] = 5.5414715350778E-17; nreg2[43] = -9.436970724121E-07;
        }

        private static void InitFieldsreg3()
        {
            ireg3[1] = 0; ireg3[2] = 0; ireg3[3] = 0; ireg3[4] = 0; ireg3[5] = 0; ireg3[6] = 0; ireg3[7] = 0; ireg3[8] = 0;
            ireg3[9] = 1; ireg3[10] = 1; ireg3[11] = 1; ireg3[12] = 1;
            ireg3[13] = 2; ireg3[14] = 2; ireg3[15] = 2; ireg3[16] = 2; ireg3[17] = 2; ireg3[18] = 2;
            ireg3[19] = 3; ireg3[20] = 3; ireg3[21] = 3; ireg3[22] = 3; ireg3[23] = 3;
            ireg3[24] = 4; ireg3[25] = 4; ireg3[26] = 4; ireg3[27] = 4;
            ireg3[28] = 5; ireg3[29] = 5; ireg3[30] = 5;
            ireg3[31] = 6; ireg3[32] = 6; ireg3[33] = 6;
            ireg3[34] = 7; ireg3[35] = 8; ireg3[36] = 9; ireg3[37] = 9; ireg3[38] = 10; ireg3[39] = 10; ireg3[40] = 11;

            jreg3[1] = 0; jreg3[2] = 0; jreg3[3] = 1; jreg3[4] = 2; jreg3[5] = 7; jreg3[6] = 10; jreg3[7] = 12; jreg3[8] = 23;
            jreg3[9] = 2; jreg3[10] = 6; jreg3[11] = 15; jreg3[12] = 17;
            jreg3[13] = 0; jreg3[14] = 2; jreg3[15] = 6; jreg3[16] = 7; jreg3[17] = 22; jreg3[18] = 26;
            jreg3[19] = 0; jreg3[20] = 2; jreg3[21] = 4; jreg3[22] = 16; jreg3[23] = 26;
            jreg3[24] = 0; jreg3[25] = 2; jreg3[26] = 4; jreg3[27] = 26;
            jreg3[28] = 1; jreg3[29] = 3; jreg3[30] = 26;
            jreg3[31] = 0; jreg3[32] = 2; jreg3[33] = 26;
            jreg3[34] = 2; jreg3[35] = 26; jreg3[36] = 2; jreg3[37] = 26; jreg3[38] = 0; jreg3[39] = 1; jreg3[40] = 26;

            nreg3[1] = 1.0658070028513; nreg3[2] = -15.732845290239; nreg3[3] = 20.944396974307; nreg3[4] = -7.6867707878716; nreg3[5] = 2.6185947787954;
            nreg3[6] = -2.808078114862; nreg3[7] = 1.2053369696517; nreg3[8] = -8.4566812812502E-03; nreg3[9] = -1.2654315477714; nreg3[10] = -1.1524407806681;
            nreg3[11] = 0.88521043984318; nreg3[12] = -0.64207765181607; nreg3[13] = 0.38493460186671; nreg3[14] = -0.85214708824206; nreg3[15] = 4.8972281541877;
            nreg3[16] = -3.0502617256965; nreg3[17] = 0.039420536879154; nreg3[18] = 0.12558408424308; nreg3[19] = -0.2799932969871; nreg3[20] = 1.389979956946;
            nreg3[21] = -2.018991502357; nreg3[22] = -8.2147637173963E-03; nreg3[23] = -0.47596035734923; nreg3[24] = 0.0439840744735; nreg3[25] = -0.44476435428739;
            nreg3[26] = 0.90572070719733; nreg3[27] = 0.70522450087967; nreg3[28] = 0.10770512626332; nreg3[29] = -0.32913623258954; nreg3[30] = -0.50871062041158;
            nreg3[31] = -0.022175400873096; nreg3[32] = 0.094260751665092; nreg3[33] = 0.16436278447961; nreg3[34] = -0.013503372241348; nreg3[35] = -0.014834345352472;
            nreg3[36] = 5.7922953628084E-04; nreg3[37] = 3.2308904703711E-03; nreg3[38] = 8.0964802996215E-05; nreg3[39] = -1.6557679795037E-04; nreg3[40] = -4.4923899061815E-05;
        }

        private static void InitFieldsreg4()
        {
            nreg4[1] = 1167.0521452767; nreg4[2] = -724213.16703206; nreg4[3] = -17.073846940092; nreg4[4] = 12020.82470247; nreg4[5] = -3232555.0322333;
            nreg4[6] = 14.91510861353; nreg4[7] = -4823.2657361591; nreg4[8] = 405113.40542057; nreg4[9] = -0.23855557567849; nreg4[10] = 650.17534844798;
        }

        private static void InitFieldsbound()
        {
            nbound[1] = 348.05185628969; nbound[2] = -1.1671859879975; nbound[3] = 1.0192970039326E-03; nbound[4] = 572.54459862746; nbound[5] = 13.91883977887;
        }

        private static void InitFieldsvisc()
        {
            n0visc[0] = 1; n0visc[1] = 0.978197; n0visc[2] = 0.579829; n0visc[3] = -0.202354;

            ivisc[1] = 0; ivisc[2] = 0; ivisc[3] = 0; ivisc[4] = 0; ivisc[5] = 1; ivisc[6] = 1; ivisc[7] = 1; ivisc[8] = 1;
            ivisc[9] = 2; ivisc[10] = 2; ivisc[11] = 2; ivisc[12] = 3; ivisc[13] = 3; ivisc[14] = 3; ivisc[15] = 3; ivisc[16] = 4;
            ivisc[17] = 4; ivisc[18] = 5; ivisc[19] = 6;

            jvisc[1] = 0; jvisc[2] = 1; jvisc[3] = 4; jvisc[4] = 5; jvisc[5] = 0; jvisc[6] = 1; jvisc[7] = 2; jvisc[8] = 3;
            jvisc[9] = 0; jvisc[10] = 1; jvisc[11] = 2; jvisc[12] = 0; jvisc[13] = 1; jvisc[14] = 2; jvisc[15] = 3; jvisc[16] = 0;
            jvisc[17] = 3; jvisc[18] = 1; jvisc[19] = 3;

            nvisc[1] = 0.5132047; nvisc[2] = 0.3205656; nvisc[3] = -0.7782567; nvisc[4] = 0.1885447; nvisc[5] = 0.2151778;
            nvisc[6] = 0.7317883; nvisc[7] = 1.241044; nvisc[8] = 1.476783; nvisc[9] = -0.2818107; nvisc[10] = -1.070786;
            nvisc[11] = -1.263184; nvisc[12] = 0.1778064; nvisc[13] = 0.460504; nvisc[14] = 0.2340379; nvisc[15] = -0.4924179;
            nvisc[16] = -0.0417661; nvisc[17] = 0.1600435; nvisc[18] = -0.01578386; nvisc[19] = -0.003629481;
        }

        private static void InitFieldsthcon()
        {
            n0thcon[0] = 1; n0thcon[1] = 6.978267; n0thcon[2] = 2.599096; n0thcon[3] = -0.998254;

            nthcon[0, 0] = 1.3293046; nthcon[0, 1] = -0.40452437; nthcon[0, 2] = 0.2440949; nthcon[0, 3] = 0.018660751; nthcon[0, 4] = -0.12961068; nthcon[0, 5] = 0.044809953;
            nthcon[1, 0] = 1.7018363; nthcon[1, 1] = -2.2156845; nthcon[1, 2] = 1.6511057; nthcon[1, 3] = -0.76736002; nthcon[1, 4] = 0.37283344; nthcon[1, 5] = -0.1120316;
            nthcon[2, 0] = 5.2246158; nthcon[2, 1] = -10.124111; nthcon[2, 2] = 4.9874687; nthcon[2, 3] = -0.27297694; nthcon[2, 4] = -0.43083393; nthcon[2, 5] = 0.13333849;
            nthcon[3, 0] = 8.7127675; nthcon[3, 1] = -9.5000611; nthcon[3, 2] = 4.3786606; nthcon[3, 3] = -0.91783782; nthcon[3, 4] = 0; nthcon[3, 5] = 0;
            nthcon[4, 0] = -1.8525999; nthcon[4, 1] = 0.9340469; nthcon[4, 2] = 0; nthcon[4, 3] = 0; nthcon[4, 4] = 0; nthcon[4, 5] = 0;
        }

        #endregion

        #region Region 1 Fundamental Equations

        private static double gammareg1(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 34; i++)
                resultado += nreg1[i] * Math.Pow((7.1 - pi), ireg1[i]) * Math.Pow((tau - 1.222), jreg1[i]);
            return resultado;
        }

        private static double gammapireg1(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 34; i++)
                resultado += -nreg1[i] * ireg1[i] * Math.Pow((7.1 - pi), (ireg1[i] - 1)) * Math.Pow((tau - 1.222), jreg1[i]);
            return resultado;
        }

        private static double gammapipireg1(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 34; i++)
                resultado += nreg1[i] * ireg1[i] * (ireg1[i] - 1) * Math.Pow((7.1 - pi), (ireg1[i] - 2)) * Math.Pow((tau - 1.222), jreg1[i]);
            return resultado;
        }

        private static double gammataureg1(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 34; i++)
                resultado += nreg1[i] * Math.Pow((7.1 - pi), ireg1[i]) * jreg1[i] * Math.Pow((tau - 1.222), (jreg1[i] - 1));
            return resultado;
        }

        private static double gammatautaureg1(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 34; i++)
                resultado += nreg1[i] * Math.Pow((7.1 - pi), ireg1[i]) * jreg1[i] * (jreg1[i] - 1) * Math.Pow((tau - 1.222), (jreg1[i] - 2));
            return resultado;
        }

        private static double gammapitaureg1(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 34; i++)
                resultado += -nreg1[i] * ireg1[i] * Math.Pow((7.1 - pi), (ireg1[i] - 1)) * jreg1[i] * Math.Pow((tau - 1.222), (jreg1[i] - 1));
            return resultado;
        }
        #endregion

        #region Region 2 Fundamental Equations

        private static double gamma0reg2(double tau, double pi)
        {
            double resultado = Math.Log(pi);
            for (int i = 1; i <= 9; i++)
                resultado += n0reg2[i] * Math.Pow(tau, j0reg2[i]);
            return resultado;
        }

        private static double gamma0pireg2(double tau, double pi)
        {
            return 1.0 / pi;
        }

        private static double gamma0pipireg2(double tau, double pi)
        {
            return -1.0 / Math.Pow(pi, 2);
        }

        private static double gamma0taureg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 9; i++)
                resultado += n0reg2[i] * j0reg2[i] * Math.Pow(tau, (j0reg2[i] - 1));
            return resultado;
        }

        private static double gamma0tautaureg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 9; i++)
                resultado += n0reg2[i] * j0reg2[i] * (j0reg2[i] - 1) * Math.Pow(tau, (j0reg2[i] - 2));
            return resultado;
        }

        private static double gamma0pitaureg2(double tau, double pi)
        {
            return 0;
        }

        private static double gammarreg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 43; i++)
                resultado += nreg2[i] * Math.Pow(pi, ireg2[i]) * Math.Pow((tau - 0.5), jreg2[i]);
            return resultado;
        }

        private static double gammarpireg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 43; i++)
                resultado += nreg2[i] * ireg2[i] * Math.Pow(pi, (ireg2[i] - 1)) * Math.Pow((tau - 0.5), jreg2[i]);
            return resultado;
        }

        private static double gammarpipireg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 43; i++)
                resultado += nreg2[i] * ireg2[i] * (ireg2[i] - 1) * Math.Pow(pi, (ireg2[i] - 2)) * Math.Pow((tau - 0.5), jreg2[i]);
            return resultado;
        }

        private static double gammartaureg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 43; i++)
                resultado += nreg2[i] * Math.Pow(pi, ireg2[i]) * jreg2[i] * Math.Pow((tau - 0.5), (jreg2[i] - 1));
            return resultado;
        }

        private static double gammartautaureg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 43; i++)
                resultado += nreg2[i] * Math.Pow(pi, ireg2[i]) * jreg2[i] * (jreg2[i] - 1) * Math.Pow((tau - 0.5), (jreg2[i] - 2));
            return resultado;
        }

        private static double gammarpitaureg2(double tau, double pi)
        {
            double resultado = 0;
            for (int i = 1; i <= 43; i++)
                resultado += nreg2[i] * ireg2[i] * Math.Pow(pi, (ireg2[i] - 1)) * jreg2[i] * Math.Pow((tau - 0.5), (jreg2[i] - 1));
            return resultado;
        }

        #endregion

        #region Region 3 Fundamental Equations

        private static double fireg3(double tau, double delta)
        {
            double resultado = nreg3[1] * Math.Log(delta);
            for (int i = 2; i <= 40; i++)
                resultado += nreg3[i] * Math.Pow(delta, ireg3[i]) * Math.Pow(tau, jreg3[i]);
            return resultado;
        }

        private static double fideltareg3(double tau, double delta)
        {
            double resultado = nreg3[1] / delta;
            for (int i = 2; i <= 40; i++)
                resultado += nreg3[i] * ireg3[i] * Math.Pow(delta, (ireg3[i] - 1)) * Math.Pow(tau, jreg3[i]);
            return resultado;
        }

        private static double fideltadeltareg3(double tau, double delta)
        {
            double resultado = -nreg3[1] / Math.Pow(delta, 2);
            for (int i = 2; i <= 40; i++)
                resultado += nreg3[i] * ireg3[i] * (ireg3[i] - 1) * Math.Pow(delta, (ireg3[i] - 2)) * Math.Pow(tau, jreg3[i]);
            return resultado;
        }

        private static double fitaureg3(double tau, double delta)
        {
            double resultado = 0;
            for (int i = 2; i <= 40; i++)
                resultado += nreg3[i] * Math.Pow(delta, ireg3[i]) * jreg3[i] * Math.Pow(tau, (jreg3[i] - 1));
            return resultado;
        }

        private static double fitautaureg3(double tau, double delta)
        {
            double resultado = 0;
            for (int i = 2; i <= 40; i++)
                resultado += nreg3[i] * Math.Pow(delta, ireg3[i]) * jreg3[i] * (jreg3[i] - 1) * Math.Pow(tau, (jreg3[i] - 2));
            return resultado;
        }

        private static double fideltataureg3(double tau, double delta)
        {
            double resultado = 0;
            for (int i = 2; i <= 40; i++)
                resultado += nreg3[i] * ireg3[i] * Math.Pow(delta, (ireg3[i] - 1)) * jreg3[i] * Math.Pow(tau, (jreg3[i] - 1));
            return resultado;
        }

        #endregion

        #region Transport Properties

        private static double psivisc(double tau, double delta)
        {
            double psi0 = 0;
            double psi1 = 0;
            for (int i = 0; i <= 3; i++)
                psi0 += n0visc[i] * Math.Pow(tau, i);

            psi0 = 1.0 / (Math.Pow(tau, 0.5) * psi0);
            double aa, bb, cc;
            int i_visc, j_visc;

            for (int i = 1; i <= 19; i++)
            {
                aa = nvisc[i];
                i_visc = ivisc[i];
                j_visc = jvisc[i];
                bb = Math.Pow(delta - 1.0, i_visc);
                cc = Math.Pow(tau - 1.0, j_visc);
                psi1 += aa * bb * cc;
            }

            psi1 = Math.Exp(delta * psi1);
            return psi0 * psi1;
        }

        private static double lambthcon(double temperature, double pressure, double tau, double delta)
        {
            double lamb0 = 0;
            double lamb1 = 0;

            for (int i = 0; i <= 3; i++)
                lamb0 += n0thcon[i] * Math.Pow(tau, i);

            lamb0 = 1.0 / (Math.Pow(tau, 0.5) * lamb0);
            for (int i = 0; i <= 4; i++)
                for (int j = 0; j <= 5; j++)
                    lamb1 += nthcon[i, j] * Math.Pow((tau - 1), i) * Math.Pow((delta - 1), j);

            lamb1 = Math.Exp(delta * lamb1);

            double psaturacion = pSatW(temperature);
            double dpidtau = 0;
            double ddeltadpi = 0;
            double taus, pis, aa, bb, deltas;

            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= psaturacion && pressure <= 1000)
            {
                taus = 1386.0 / temperature;
                pis = pressure / 165.3;
                aa = 647.226 * 165.3 * (gammapitaureg1(taus, pis) * 1386.0 - gammapireg1(taus, pis) * temperature);
                bb = (221.15 * Math.Pow(temperature, 2.0) * gammapipireg1(taus, pis));
                dpidtau = aa / bb;
                aa = -(22115000.0 * gammapipireg1(taus, pis));
                bb = (317.763 * rgas_water * temperature * Math.Pow(gammapireg1(taus, pis), 2));
                ddeltadpi = aa / bb;
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= psaturacion) ||
                (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature)) ||
                (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000))
            {
                taus = 540.0 / temperature;
                pis = pressure / 10.0;
                aa = (647.226 * 10.0 * ((gamma0pitaureg2(taus, pis) + gammarpitaureg2(taus, pis)) * 54.0 - (gamma0pireg2(taus, pis) + gammarpireg2(taus, pis)) * temperature));
                bb = (221.15 * Math.Pow(temperature, 2.0) * (gamma0pipireg2(taus, pis) + gammarpipireg2(taus, pis)));
                dpidtau = aa / bb;
                aa = -(22115000.0 * (gamma0pipireg2(taus, pis) + gammarpipireg2(taus, pis)));
                bb = (317.763 * rgas_water * temperature * Math.Pow((gamma0pireg2(taus, pis) + gammarpireg2(taus, pis)), 2));
                ddeltadpi = aa / bb;
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000)
            {
                taus = 647.096 / temperature;
                deltas = delta * 317.763 / 322.0;
                aa = (647.226 * rgas_water * Math.Pow((delta * 317.763), 2) * (fideltareg3(taus, deltas) - (647.096 / temperature) * fideltataureg3(taus, deltas)));
                bb = (22115000.0 * 322.0);
                dpidtau = aa / bb;
                aa = bb;
                bb = (317.763 * delta * 317.763 * rgas_water * temperature * (2 * fideltareg3(taus, deltas) + (delta * 317.763 / 322.0) * fideltadeltareg3(taus, deltas)));
                ddeltadpi = aa / bb;
            }
            else
            {
                dpidtau = 0;
                ddeltadpi = 0;
            }

            aa = 0.0013848 / psivisc(tau, delta);
            bb = Math.Exp(-18.66 * Math.Pow(1.0 / tau - 1.0, 2) - Math.Pow(delta - 1.0, 4));
            double cc = Math.Pow(tau * delta, -2.9) * Math.Pow(dpidtau, 2) * Math.Pow(delta * ddeltadpi, 0.4678) * Math.Pow(delta, 0.5);
            double lamb2 = aa * cc * bb;
            return lamb0 * lamb1 + lamb2;
        }

        #endregion

        #region Saturation & Boundary Logic

        public static double pSatW(double temperature)
        {
            if (temperature < 273.15 || temperature > 647.096)
                return -1;

            double del = temperature + nreg4[9] / (temperature - nreg4[10]);
            double aco = Math.Pow(del, 2) + nreg4[1] * del + nreg4[2];
            double bco = nreg4[3] * Math.Pow(del, 2) + nreg4[4] * del + nreg4[5];
            double cco = nreg4[6] * Math.Pow(del, 2) + nreg4[7] * del + nreg4[8];
            double aa = Math.Pow(bco * bco - 4 * aco * cco, 0.5);
            return Math.Pow(2 * cco / (-bco + aa), 4) * 10;
        }

        public static double tSatW(double pressure)
        {
            if (pressure < 0.00611213 || pressure > 220.64)
                return -1;

            double bet = Math.Pow((0.1 * pressure), 0.25);
            double eco = Math.Pow(bet, 2) + nreg4[3] * bet + nreg4[6];
            double fco = nreg4[1] * Math.Pow(bet, 2) + nreg4[4] * bet + nreg4[7];
            double gco = nreg4[2] * Math.Pow(bet, 2) + nreg4[5] * bet + nreg4[8];
            double dco = 2 * gco / (-fco - Math.Pow((fco * fco - 4 * eco * gco), 0.5));
            return 0.5 * (nreg4[10] + dco - Math.Pow(Math.Pow(nreg4[10] + dco, 2) - 4 * (nreg4[9] + nreg4[10] * dco), 0.5));
        }

        public static double tBound(double pressure)
        {
            if (pressure < 165.292 || pressure > 1000)
                return -1;

            return nbound[4] + Math.Pow(((0.1 * pressure - nbound[5]) / nbound[3]), 0.5);
        }

        public static double pBound(double temperature)
        {
            if (temperature < 623.15 || temperature > 863.15)
                return -1;

            return (nbound[1] + nbound[2] * temperature + nbound[3] * Math.Pow(temperature, 2)) * 10.0;
        }

        private static double densreg3(double temperature, double pressure)
        {
            double densold = 0;
            if (temperature < tc_water && pressure < pSatW(temperature))
                densold = 100.0;
            else
                densold = 600.0;

            double tau = tc_water / temperature;
            double delta, derivprho, densnew, diffdens;

            for (int j = 1; j < 1000; j++)
            {
                delta = densold / dc_water;
                derivprho = rgas_water * temperature / dc_water * (2 * densold * fideltareg3(tau, delta) + Math.Pow(densold, 2) / dc_water * fideltadeltareg3(tau, delta));
                densnew = densold + (pressure * 100000.0 - rgas_water * temperature * Math.Pow(densold, 2) / dc_water * fideltareg3(tau, delta)) / derivprho;
                diffdens = Math.Abs(densnew - densold);
                if (diffdens < 0.000005)
                {
                    return densnew;
                }
                densold = densnew;
            }
            return -2.0;
        }

        #endregion

        #region Region Properties Methods

        private static double volreg1(double temperature, double pressure)
        {
            double tau = 1386.0 / temperature;
            double pi = 0.1 * pressure / 16.53;
            return rgas_water * temperature * pi * gammapireg1(tau, pi) / (pressure * 100000.0);
        }

        private static double energyreg1(double temperature, double pressure)
        {
            double tau = 1386.0 / temperature;
            double pi = 0.1 * pressure / 16.53;
            return 0.001 * rgas_water * temperature * (tau * gammataureg1(tau, pi) - pi * gammapireg1(tau, pi));
        }

        private static double entropyreg1(double temperature, double pressure)
        {
            double tau = 1386.0 / temperature;
            double pi = 0.1 * pressure / 16.53;
            return 0.001 * rgas_water * (tau * gammataureg1(tau, pi) - gammareg1(tau, pi));
        }

        private static double enthalpyreg1(double temperature, double pressure)
        {
            double tau = 1386.0 / temperature;
            double pi = 0.1 * pressure / 16.53;
            return 0.001 * rgas_water * temperature * tau * gammataureg1(tau, pi);
        }

        private static double cpreg1(double temperature, double pressure)
        {
            double tau = 1386.0 / temperature;
            double pi = 0.1 * pressure / 16.53;
            return -0.001 * rgas_water * tau * tau * gammatautaureg1(tau, pi);
        }

        private static double cvreg1(double temperature, double pressure)
        {
            double tau = 1386.0 / temperature;
            double pi = 0.1 * pressure / 16.53;
            return 0.001 * rgas_water * (-tau * tau * gammatautaureg1(tau, pi) + Math.Pow(gammapireg1(tau, pi) - tau * gammapitaureg1(tau, pi), 2) / gammapipireg1(tau, pi));
        }

        private static double volreg2(double temperature, double pressure)
        {
            double tau = 540.0 / temperature;
            double pi = 0.1 * pressure;
            return rgas_water * temperature * pi * (gamma0pireg2(tau, pi) + gammarpireg2(tau, pi)) / (pressure * 100000.0);
        }

        private static double energyreg2(double temperature, double pressure)
        {
            double tau = 540.0 / temperature;
            double pi = 0.1 * pressure;
            return 0.001 * rgas_water * temperature * (tau * (gamma0taureg2(tau, pi) + gammartaureg2(tau, pi)) - pi * (gamma0pireg2(tau, pi) + gammarpireg2(tau, pi)));
        }

        private static double entropyreg2(double temperature, double pressure)
        {
            double tau = 540.0 / temperature;
            double pi = 0.1 * pressure;
            return 0.001 * rgas_water * (tau * (gamma0taureg2(tau, pi) + gammartaureg2(tau, pi)) - (gamma0reg2(tau, pi) + gammarreg2(tau, pi)));
        }

        private static double enthalpyreg2(double temperature, double pressure)
        {
            double tau = 540.0 / temperature;
            double pi = 0.1 * pressure;
            return 0.001 * rgas_water * temperature * tau * (gamma0taureg2(tau, pi) + gammartaureg2(tau, pi));
        }

        private static double cpreg2(double temperature, double pressure)
        {
            double tau = 540.0 / temperature;
            double pi = 0.1 * pressure;
            return -0.001 * rgas_water * tau * tau * (gamma0tautaureg2(tau, pi) + gammartautaureg2(tau, pi));
        }

        private static double cvreg2(double temperature, double pressure)
        {
            double tau = 540.0 / temperature;
            double pi = 0.1 * pressure;
            double aa = -Math.Pow(tau, 2) * (gamma0tautaureg2(tau, pi) + gammartautaureg2(tau, pi));
            double bb = Math.Pow((1 + pi * gammarpireg2(tau, pi) - tau * pi * gammarpitaureg2(tau, pi)), 2);
            double cc = (1 - Math.Pow(pi, 2) * gammarpipireg2(tau, pi));
            return 0.001 * rgas_water * (aa - bb / cc);
        }

        private static double pressreg3(double temperature, double density)
        {
            double tau = tc_water / temperature;
            double delta = density / dc_water;
            return density * rgas_water * temperature * delta * fideltareg3(tau, delta) / 100000.0;
        }

        private static double energyreg3(double temperature, double density)
        {
            double tau = tc_water / temperature;
            double delta = density / dc_water;
            return 0.001 * rgas_water * temperature * tau * fitaureg3(tau, delta);
        }

        private static double entropyreg3(double temperature, double density)
        {
            double tau = tc_water / temperature;
            double delta = density / dc_water;
            return 0.001 * rgas_water * (tau * fitaureg3(tau, delta) - fireg3(tau, delta));
        }

        private static double enthalpyreg3(double temperature, double density)
        {
            double tau = tc_water / temperature;
            double delta = density / dc_water;
            return 0.001 * rgas_water * temperature * (tau * fitaureg3(tau, delta) + delta * fideltareg3(tau, delta));
        }

        private static double cpreg3(double temperature, double density)
        {
            double tau = tc_water / temperature;
            double delta = density / dc_water;
            double aa = -tau * tau * fitautaureg3(tau, delta);
            double bb = Math.Pow(delta * fideltareg3(tau, delta) - delta * tau * fideltataureg3(tau, delta), 2);
            double cc = (2 * delta * fideltareg3(tau, delta) + delta * delta * fideltadeltareg3(tau, delta));
            return 0.001 * rgas_water * (aa + bb / cc);
        }

        private static double cvreg3(double temperature, double density)
        {
            double tau = tc_water / temperature;
            double delta = density / dc_water;
            return 0.001 * rgas_water * (-tau * tau * fitautaureg3(tau, delta));
        }

        #endregion

        #region Public Properties API

        public static double densW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= pSatW(temperature) && pressure <= 1000.0)
            {
                return 1.0 / volreg1(temperature, pressure);
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= pSatW(temperature))
                || (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature))
                || (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000.0))
            {
                return 1.0 / volreg2(temperature, pressure);
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000.0)
            {
                return densreg3(temperature, pressure);
            }
            return -1.0;
        }

        public static double energyW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= pSatW(temperature) && pressure <= 1000.0)
            {
                return energyreg1(temperature, pressure);
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= pSatW(temperature)) ||
                (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature)) ||
                (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000.0))
            {
                return energyreg2(temperature, pressure);
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000.0)
            {
                double density = densreg3(temperature, pressure);
                return energyreg3(temperature, density);
            }
            return -1;
        }

        public static double entropyW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= pSatW(temperature) && pressure <= 1000)
            {
                return entropyreg1(temperature, pressure);
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= pSatW(temperature))
                || (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature))
                || (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000))
            {
                return entropyreg2(temperature, pressure);
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000)
            {
                double density = densreg3(temperature, pressure);
                return entropyreg3(temperature, density);
            }
            return -1;
        }

        public static double enthalpyW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= pSatW(temperature) && pressure <= 1000)
            {
                return enthalpyreg1(temperature, pressure);
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= pSatW(temperature)) ||
                (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature)) ||
                (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000))
            {
                return enthalpyreg2(temperature, pressure);
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000)
            {
                double density = densreg3(temperature, pressure);
                return enthalpyreg3(temperature, density);
            }
            return -1;
        }

        public static double cpW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= pSatW(temperature) && pressure <= 1000)
            {
                return cpreg1(temperature, pressure);
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= pSatW(temperature)) ||
                (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature)) ||
                (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000))
            {
                return cpreg2(temperature, pressure);
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000)
            {
                double density = densreg3(temperature, pressure);
                return cpreg3(temperature, density);
            }
            return -1;
        }

        public static double cvW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 623.15 && pressure >= pSatW(temperature) && pressure <= 1000)
            {
                return cvreg1(temperature, pressure);
            }
            else if ((temperature >= 273.15 && temperature <= 623.15 && pressure > 0 && pressure <= pSatW(temperature)) ||
                (temperature >= 623.15 && temperature <= 863.15 && pressure > 0 && pressure <= pBound(temperature)) ||
                (temperature >= 863.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000))
            {
                return cvreg2(temperature, pressure);
            }
            else if (temperature >= 623.15 && temperature <= tBound(pressure) && pressure >= pBound(temperature) && pressure <= 1000)
            {
                double density = densreg3(temperature, pressure);
                return cvreg3(temperature, density);
            }
            return -1;
        }

        public static double viscW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000)
            {
                double density = densW(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            return -1;
        }

        public static double thconW(double temperature, double pressure)
        {
            if (temperature >= 273.15 && temperature <= 1073.15 && pressure > 0 && pressure <= 1000)
            {
                double density = densW(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            return -1;
        }

        #endregion

        #region Saturation Properties (Liq & Vap based on T & P)

        public static double densSatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return 1.0 / volreg1(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                return densreg3(temperature, pressure);
            }
            return -1;
        }

        public static double densSatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return 1.0 / volreg2(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                return densreg3(temperature, pressure);
            }
            return -1;
        }

        public static double densSatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return 1.0 / volreg1(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                return densreg3(temperature, pressure);
            }
            return -1;
        }

        public static double densSatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return 1.0 / volreg2(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                return densreg3(temperature, pressure);
            }
            return -1;
        }

        public static double energySatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return energyreg1(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                return energyreg3(temperature, density);
            }
            return -1;
        }

        public static double energySatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return energyreg2(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                return energyreg3(temperature, density);
            }
            return -1;
        }

        public static double energySatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return energyreg1(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                return energyreg3(temperature, density);
            }
            return -1;
        }

        public static double energySatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return energyreg2(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                return energyreg3(temperature, density);
            }
            return -1;
        }

        public static double entropySatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return entropyreg1(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                return entropyreg3(temperature, density);
            }
            return -1;
        }

        public static double entropySatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return entropyreg2(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                return entropyreg3(temperature, density);
            }
            return -1;
        }

        public static double entropySatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return entropyreg1(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                return entropyreg3(temperature, density);
            }
            return -1;
        }

        public static double entropySatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return entropyreg2(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                return entropyreg3(temperature, density);
            }
            return -1;
        }

        public static double enthalpySatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return enthalpyreg1(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                return enthalpyreg3(temperature, density);
            }
            return -1;
        }

        public static double enthalpySatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return enthalpyreg2(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                return enthalpyreg3(temperature, density);
            }
            return -1;
        }

        public static double enthalpySatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return enthalpyreg1(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                return enthalpyreg3(temperature, density);
            }
            return -1;
        }

        public static double enthalpySatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return enthalpyreg2(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                return enthalpyreg3(temperature, density);
            }
            return -1;
        }

        public static double cpSatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return cpreg1(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                return cpreg3(temperature, density);
            }
            return -1;
        }

        public static double cpSatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return cpreg2(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                return cpreg3(temperature, density);
            }
            return -1;
        }

        public static double cpSatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return cpreg1(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                return cpreg3(temperature, density);
            }
            return -1;
        }

        public static double cpSatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return cpreg2(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                return cpreg3(temperature, density);
            }
            return -1;
        }

        public static double cvSatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return cvreg1(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                return cvreg3(temperature, density);
            }
            return -1;
        }

        public static double cvSatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                return cvreg2(temperature, pressure);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                return cvreg3(temperature, density);
            }
            return -1;
        }

        public static double cvSatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return cvreg1(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                return cvreg3(temperature, density);
            }
            return -1;
        }

        public static double cvSatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                return cvreg2(temperature, pressure);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                return cvreg3(temperature, density);
            }
            return -1;
        }

        public static double viscSatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                double density = 1.0 / volreg1(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            return -1;
        }

        public static double viscSatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                double density = 1.0 / volreg2(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            return -1;
        }

        public static double viscSatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                double density = 1.0 / volreg1(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            return -1;
        }

        public static double viscSatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                double density = 1.0 / volreg2(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.000055071 * psivisc(tau, delta);
            }
            return -1;
        }

        public static double thconSatLiqTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                double density = 1.0 / volreg1(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature);
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            return -1;
        }

        public static double thconSatVapTW(double temperature)
        {
            if (temperature >= 273.15 && temperature <= 623.15)
            {
                double pressure = pSatW(temperature);
                double density = 1.0 / volreg2(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                pressure = pressure - 0.0001 * pressure;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            else if (temperature > 623.15 && temperature <= tc_water)
            {
                double pressure = pSatW(temperature) - 0.00001;
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            return -1;
        }

        public static double thconSatLiqPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                double density = 1.0 / volreg1(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure += 0.00001;
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            return -1;
        }

        public static double thconSatVapPW(double pressure)
        {
            if (pressure >= pSatW(273.15) && pressure <= pSatW(623.15))
            {
                double temperature = tSatW(pressure);
                double density = 1.0 / volreg2(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                pressure = pressure - 0.0001 * pressure;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            else if (pressure > pSatW(623.15) && pressure <= pc_water)
            {
                double temperature = tSatW(pressure);
                pressure -= 0.00001;
                double density = densreg3(temperature, pressure);
                double delta = density / 317.763;
                double tau = 647.226 / temperature;
                return 0.4945 * lambthcon(temperature, pressure, tau, delta);
            }
            return -1;
        }

        #endregion
    }
}
