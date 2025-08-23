import sqlite3

ALTERNATIVE = 0
CARD = 2

cocaceaQuestions = [
    # Cocaceas,
    (
        "Especie bacteriana que, en general, se asocia con la formación de abscesos e infecciones de la piel",
        CARD,
        0,
    ),
    (
        "Principal especie bacteriana que presenta una enzima catalizadora de la conversión de fibrinógeno en fibrina y genera cuadros clínico en humanos",
        CARD,
        0,
    ),
    (
        "Especie bacteriana conocida por formar racimos de cocaceas y puede causar infecciones cutáneas y sistemicas en humanos",
        CARD,
        0,
    ),
    (
        "Especie de bacteria que es agente común de infecciones del torrente sanguíeno asociadas con catéteres intravenosos y dispositivos médicos implantados",
        CARD,
        0,
    ),
    (
        "Especie bacteriana que habita el tracto gastrointestinal humano y es causante de infecciones del tracto urinario en mujeres sexualmente activas",
        CARD,
        0,
    ),
    (
        "Especie bacteriana que debe ser pesquisada en mujeres embarazadas de 35 a 37 semanas, por el riesgo de producir infecciones invasivas en neonatos",
        CARD,
        0,
    ),
    (
        "Agente causal de faringoamigdalitis y fiebre escarlatina en seres humanos",
        CARD,
        0,
    ),
    (
        "Especie bacteriana alfa-hemolítica causante de abscesos dentales y endocarditis en seres humanos",
        CARD,
        0,
    ),
    ("Diplococo de aspecto lanceolado causante de neumonía bacteriana", CARD, 0),
    (
        "Especie bacteriana causante de fascitis necrozante tipo II (monomicrobiana)",
        CARD,
        0,
    ),
    (
        "¿Qué especie bacteriana es comúnmente encontrada en la microbiota gastrointestinal humana y puede causar infecciones del tracto urinario y bacteriemias",
        CARD,
        0,
    ),
    (
        "¿Cuál de las siguientes especies bacterianas es conocida por su resistencia intrínseca a múltiples antibióticos, incluyendo la vancomicina",
        CARD,
        0,
    ),
    (
        "¿Qué especie bacteriana es una gente causal de endocarditis bacteriana, especialmente en pacientes con válvlulas cardiácas artificiales?",
        CARD,
        0,
    ),
    (
        "Especie bacteriana que en  medio telurito de potasio da como resultado colonias negras",
        CARD,
        0,
    ),
    ("Bacteria cocácea que  presenta pigmento amarillo en Agar Sangre", CARD, 0),
    (
        "Especie bacteriana responsable de la meningitis bacteriana y séptica.",
        CARD,
        0,
    ),
    ("Diplococo Gram negativo que presenta una cápsula polisacárida", CARD, 0),
    (
        "Bacteria de morfología cocacea, agente etiológico de una infección de transmisión sexual.",
        CARD,
        0,
    ),
    (
        "El medio de cultivo Thayer Martin es utilizado para el aislamiento de:",
        CARD,
        0,
    ),
    (
        "¿Cuál de las siguientes especies de bacteria es parte de la microbiota normal de las vías resppiratorias superiores pero puede causar infecciones como faringitis y otitis media?",
        CARD,
        0,
    ),
]  # 1 - 20

bacGranNegQuestions = [
    (
        "¿Cuál de los siguientes bacilos Gram negativos es conocido por ser una causa común de infecciones del tracto urinario en huumanos?",
        CARD,
        0,
    ),
    ("Patógeno cuyo principal mecanismo de resistencia es la enzima KPC", CARD, 0),
    (
        "La peste bubónica es una afección causada por un microorganismo que pertenece al grupo de  ",
        CARD,
        0,
    ),
    (
        "Las bacterias que son capaces de producir la reacción de desaminación de la lisina en el medio LIA pertenecen al grupo de",
        CARD,
        0,
    ),
    (
        "¿Cuál de los siguientes bacilos gram negativos es una causa frecuente de infecciones gastrointestinales transmitidas por alimentos contaminados y se caracteriza por la producción de toxinas que afectan el intestino?",
        CARD,
        0,
    ),
    (
        "Bacilos Gram negativo  aerobios, que no fermentan hidratos de carbono como fuente de energia ",
        CARD,
        0,
    ),
    (
        "¿Cuál de los siguientes bacilos gram negativos es conocido por ser un patógeno oportunista en infeccioes hospitalarias y es resistente a muchos antibióticos? ",
        CARD,
        0,
    ),
    (
        "Son agentes etiológicos frecuentes de infecciones adquiridas en el hospital y patógenos oportunistas en pacientes inmunodeprimidos, sometidos a procederes invasivos",
        CARD,
        0,
    ),
    (
        "Bacilo Gram negativo conocido por ser un contaminante común en soluciones salinas y desinfectantes, y puede causar infecciones en pacientes inmunocomprometidos",
        CARD,
        0,
    ),
    ("Bacilo Gram negativo productor de piocianina ", CARD, 0),
    (
        "Bacilos Gram negativo curvo, oxidasa positivo, halófilo y generalmente móviles por un único flagelo polar ",
        CARD,
        0,
    ),
    (
        "Especie bacteriana  causante de intoxicaciones alimentaria a través del consumo de mariscos crudos. ",
        CARD,
        0,
    ),
    (
        'Patógeno que produce cuadros abruptos de diarra líquida profusa descrita como "agua de arroz"',
        CARD,
        0,
    ),
    (
        "Grupo al que pertenece el microorganismo capaz de causar infecciones por heridas contaminadas con agua salada o marina, llevando a celulitos y septicemia",
        CARD,
        0,
    ),
    (
        "El medio de cultivo selectivo diferencial conocido como TCB se utiliza frecuentemente para el aislamiento de especies de: ",
        CARD,
        0,
    ),
    ("Bacilo Gram negativo curvo con forma espirilada", CARD, 0),
    (
        "Especie bacteriana causante de Enfermedad de transmisión alimentaria (ETA) por consumo de carne de pollo mal cocida o cruda",
        CARD,
        0,
    ),
    ("Bacteria que se asocia al Sindrome de Guillain Barré", CARD, 0),
    ("Bacteria de desarrollo óptimo a 42°C y en un ambiente microaerofílico", CARD, 0),
    (
        "La tinción de Hucker en muestras de heces se realiza para la pesquiza de: ",
        CARD,
        0,
    ),
]  # 21 - 40

hongosQuestions = [
    (
        "¿Cuál de las siguientes agrupaciones de hongos es más conocida por causar infecciones oportunistas en pacientes inmunocomprometidos, especialmente en forma de candidiasis",
        CARD,
        0,
    ),
    (
        "Grupo de hongos que puede fermentar azúcares, una característica que se aprovecha en la producción de alimentos y bebidas como el pan y la cerveza.",
        CARD,
        0,
    ),
    ("Grupo de hongos que incluye especies como Candida y Cryptococcus", CARD, 0),
    (
        "Grupo de hongo unicelular que se reproduce por yemagicón o fisión binaria.",
        CARD,
        0,
    ),
    ("Grupo de hongos con uso industrial utilizada como probiótico", CARD, 0),
    (
        "Especie fúngica termotolerante, que produce colonias verde azuladas, pulverulentas y de borde blanco.",
        CARD,
        0,
    ),
    (
        "Grupo de hongos que se caracteriza por tener una estructura conocida como fíalide  y produce conidios.",
        CARD,
        0,
    ),
    ("Género fúngico productor de aflatoxinas", CARD, 0),
    (
        "Grupo de hongo que se caracteriza por tener una hifa que se conoce como célula pie",
        CARD,
        0,
    ),
    (
        "Grupo de hongos que incluye especies que son conocidas por su capacidad de producir micotoxinas y pueden contaminar alimentos, causando enfermedades",
        CARD,
        0,
    ),
    (
        "Grupo de hongos que posee una estructura que tiene aspecto de raiz y que se adhiere a los sustratos",
        CARD,
        0,
    ),
    (
        "Grupo de hongos que posee una estructura en forma de saco que en su interior contiene a las esporangioesporas.",
        CARD,
        0,
    ),
    (
        "¿Qué grupo de hongos se caracteriza por tener hifas anchas, no tabicadas y de crecimiento rápido?",
        CARD,
        0,
    ),
    (
        "¿Qué grupo de hongos es conocida por causar infecciones rinocerebrales, pulmonares y cutáneas en personas inmunocomprometidas?",
        CARD,
        0,
    ),
    (
        "¿Ciáles de los siguientes grupos de hongos se caracteriza por tener esporangios que producen esporas asexuales en su interior?",
        CARD,
        0,
    ),
    (
        "Grupo de hongos que se caracteriza por tener hifas pigmentadas debido a la presencia de melanina.",
        CARD,
        0,
    ),
    (
        "La Alternaria es un hongo que produce macroconidios con septos longitudinales y transversales dispuestos en cadenas y que pertenece al grupo de los:",
        CARD,
        0,
    ),
    (
        "Hongo que produce macroconidios con septos transversales siendo su tercera célula la más ancha.",
        CARD,
        0,
    ),
    (
        "El cuadro clínico conocido como  cromoblastomicosis es producido por un hongo que pertenece al grupo:",
        CARD,
        0,
    ),
    (
        "La feohifomicosis es un cuadro producido por hongos pertenecientes al grupo de:",
        CARD,
        0,
    ),
]  # 41 - 60

bacGranPosQuestions = [
    (
        "Bacteria Gram positio productora de Enfermedad de Transmision Alimentaria (ETA) con capacidad de crecer a temperaturas de refrigeración (4ºC)",
        CARD,
        0,
    ),
    (
        "Especie de bacilo gram positivo conocido por causar infecciones graves en recién nacidos y personas inmunocomprometidas. ",
        CARD,
        0,
    ),
    (
        "Bacilo Gram positivo que presenta una resistencia natural a cefalosporinas, fosfomicina y quinolonas",
        CARD,
        0,
    ),
    ("Bacilo Gram positivo en empalizada, β-hemolítico y CAMP positivo ", CARD, 0),
    ("Microorganismo causante de Granulomatosis Infantoséptica", CARD, 0),
    (
        "Bacilos Gram positivo, ligeramente curvos y con orientación angular de letra “v” o letras chinas ",
        CARD,
        0,
    ),
    (
        "Especie bacteriana que se caracteriza por producir una reacción positiva al test de Urea en pocos minutos ",
        CARD,
        0,
    ),
    ("Microorganismo productor de la toxina diftérica ", CARD, 0),
    (
        "De este microorganismo se distinguen 4 biovariedades: gravis, intermedius, mitis, belfanti",
        CARD,
        0,
    ),
    (
        "Especie bacteriana que produce cistitis incrustante y se presenta con orina de pH alcalino",
        CARD,
        0,
    ),
    (
        "Bacilo Gram positivo formador de esporas asociado a Enfermedad de Transmisión Alimentaria (ETA)",
        CARD,
        0,
    ),
    (
        "Bacilo Gram positivo esporoformador que produce colonias β- hemolíticas de aspecto rugoso",
        CARD,
        0,
    ),
    ("Microorganismo esporulado causante del Carbunco", CARD, 0),
    ("Microorganismo productor de toxina emética", CARD, 0),
    (
        "Bacilo Gram positivo entomopatógeno capaz de producir cristal paraesporal utilizado comunmente como bioinsecticida",
        CARD,
        0,
    ),
    (
        "Bacilo Gram positivo filamentoso ligeramente curvado que genera colonias puntiformes en Agar Sangre a las 24 horas de incubación",
        CARD,
        0,
    ),
    (
        "Bacteria Gram positiva que puede infectar tanto a humanos como a animales, especialmente cerdos, causando endocarditis en humanos.",
        CARD,
        0,
    ),
    (
        "Bacilo Gram positivo que en el medio de cultivo Hierro Triple Azúcar (TSI) produce H2S",
        CARD,
        0,
    ),
    (
        "Microorganismo responsable de zoonosis transmitida principalmente por manipulación de carne de cerdo",
        CARD,
        0,
    ),
    ('Microorganismos causante del "Mal rojo del cerdo"', CARD, 0),
]  # 61 - 80

anaerobiosQuestions = [
    (
        "Bacilo anaerobio esporulado que puede formar parte de la microbiota intestinal de humanos y animales",
        CARD,
        0,
    ),
    (
        "Especie de Clostridioides asociado brotes de diarreas intrahospitalarias",
        CARD,
        0,
    ),
    (
        "La ampicilina, es un antibótico que frecuentemente se asocia a los cuadros de diarrea  en portadores del microorganismo anaerobio: ",
        CARD,
        0,
    ),
    (
        "Microorganismo anaerobio cuyo cuadri clínico a nivel intestinal es producido por la toxina A y B ",
        CARD,
        0,
    ),
    ("Especie de Clostridioides causante de colitis pseudomembranosa", CARD, 0),
    ("Especie de Clostridium cuya espora resiste la ebullición por horas", CARD, 0),
    (
        "Especie de Clostridium de notificación inmediata según la circular de Enfermedad de Notificación Obligatoria (ENO) del Ministerio de Salud",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium cuyo cuadro clínico se caracteriza por producir parálisis flácida. ",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium responsable de producri una neurotooxina potente, utilizada en pequeñas dosis en medicina estética para reducir arrugas:",
        CARD,
        0,
    ),
    (
        "Microorganismos anaerobio del género Clostridium capaz de formar esporas y producir una toxina que bloquea la liberación de acetilcolina en la sinapsis neuromuscular: ",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium conocida por porducir una toxina que se utiliza terapéuticamente para tratar efecciones como espasmos musculares ymigrañas crónicas",
        CARD,
        0,
    ),
    (
        "Bacilo Gram positivo productor de espora en posición terminal que le otorga un aspecto de palillo de tambor ",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium causante de una toxina que bloquea la liberación de neurotransmisores inhibitorios, causando rigidez y espasmos musculates",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium causante de una enfermedad que se caracteriza por producir espasmos musculares severos:",
        CARD,
        0,
    ),
    (
        "Microorganismo cuya toxina produce sinapsis contínua y contracciones musculares que conllevan una hiperreflexia extrema y espasmos violentos del músculo esquelético",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium responsable de una infección que se contrar típicamente a través de heridas contaminadas y puede llevar a constracciones musculares dolorosas",
        CARD,
        0,
    ),
    ("Especie de Clostridium caratcerizado por ser inmóvil", CARD, 0),
    ("Especie de Clostridium productor del cuadro clínico Gangrena gaseosa ", CARD, 0),
    (
        "Especie de Clostridium productor de Intoxicación alimentaria por consumo de productos cárnicos",
        CARD,
        0,
    ),
    (
        "Especie de Clostridium conocida por producir enterotioxinas que pueden causar infecciones graves de los tejidos blandos.",
        CARD,
        0,
    ),
    (
        "Clostridium capaz de producir alfa toxina, responsable de la lisisi de eritrocitos, plaquetas y células endoteliales",
        CARD,
        0,
    ),
]  # 81 - 101

mycobacQuestions = [
    ("Especie bacteriana que produce la Enfermedad de Hansen", CARD, 0),
    ("Especie de Micobacteria que no es cultivable", CARD, 0),
    (
        "Especie bacteriana que requiere de largos periodos de incubación (promedio de 2 a 4 años) en el hospedero",
        CARD,
        0,
    ),
    (
        "Micobacteria que tiene preferencia por temperaturas bajas, infectando principalmente áreas del cuerpo como la piel, los nervios periféricos y el tracto respiratorio superior. ",
        CARD,
        0,
    ),
    (
        "Micobacteria responsable de una enfermedad que históricamente ha sido estigmatizante debido a sus manifestaciones físicas visibles y crónicas.",
        CARD,
        0,
    ),
    ("Agente etiológico de Tuberculosis Bovina:", CARD, 0),
    ("Especie de Micobacteria que tiene comportamiento zoonótico", CARD, 0),
    (
        "Especie de Micobacteria que  se encuentra principalmente en animales y puede infectar a humanos, principalmente a través del consumo de productos lácteros contaminados. ",
        CARD,
        0,
    ),
    (
        "Micobacteria capaz de producir una infección pulmonar en humanos que es clínicamente indistinguible de la tuberculosis producida por M. tuberculosis",
        CARD,
        0,
    ),
    ("Especie de Micobacteria utilizada para desarrollar  la vacuna BCG ", CARD, 0),
    (
        "Especie bacteriana que requiere tratamiento combinado de Rifampicina,  Etambutol,  Isoniacida y Pirazinamida debido a su resistencia a múltiples fármacos",
        CARD,
        0,
    ),
    (
        "Especie de Micobateria conocida por su capacidad de latencia prolongada en el cuerpo humano y que puede reactivarse años después de la infección inicial",
        CARD,
        0,
    ),
    (
        "Micobacteria responsable de la enfermedad infecciosa que afecta principalmente a los pulmones pero puede diseminarse a otros órganos como los riñones, el cerebro y la columna vertebral.",
        CARD,
        0,
    ),
    (
        "Micobacteria cuya principal vía de transmisión es por medio de aire contminado por una persona infectada que tose, estornuda o habla.",
        CARD,
        0,
    ),
    (
        "Especie de micobacteria que pudiese ser detectada mediantre la pruba de derivado protéico purificado en la piel",
        CARD,
        0,
    ),
    (
        "Complejo bacteriano al que pertenece la especie de micobacteria que produce Ulcera de Buruli",
        CARD,
        0,
    ),
    ("Complejo bacteriano al cual pertenece M. fortuitum ", CARD, 0),
    (
        "Complejo de micobacterias al que pertenece Mycobacteriym avium, Mycobacterium kansasii y Mycobacterium marinum",
        CARD,
        0,
    ),
    (
        "Complejo de micobacterias cuyas especies se encuentran generalmente en el medio ambiente, incluyendo agua y suelo.",
        CARD,
        0,
    ),
    (
        "Complejo de micobacterias conocido por incluir especies que causan infecciones cutáneas después de la exposición a ambientes acuáticos contaminados. ",
        CARD,
        0,
    ),
]  # 102 - 121

medioCultivo = [
    (
        "Medio de cultivo cuyo indicador de pH es el rojo fenol y tiene un pH de 7,3",
        CARD,
        0,
    ),
    (
        "Medio de cultivo utilziado para diferenciar bacterias basándose en la fermentación de la lactosa, sacarosa y glucosa",
        CARD,
        0,
    ),
    (
        "Medio de cultivo diferencial, cuyo indicador de pH es el rojo fenol y permite visualizar la producción de Ácido sulfhídrico.",
        CARD,
        0,
    ),
    (
        "Medio de cultivo que, cuando el microorganismo fermenta la glucosa, se visualiza de un color rojo en la superficie y blanco en el fondo",
        CARD,
        0,
    ),
    (
        "Medio de cultivo en el cual se puede visualizar de mejor modo la producción de gas, a partir de la fermentación de azúcares",
        CARD,
        0,
    ),
    (
        "Medio de cultivo sólido, cuyo indicador de pH es el púrpura de bromocresol",
        CARD,
        0,
    ),
    ("Medio de cultivo en el que se puede obtener ácido α-ceto carbónico", CARD, 0),
    (
        "Medio de cultivo en el cual el producto de reacción del metabolismo bacteriano es cadaverina",
        CARD,
        0,
    ),
    (
        "Medio de cultivo en el cual se puede observarla reacción de desaminación de un aminoácido",
        CARD,
        0,
    ),
    (
        "Medio de cultivo cuy Indicador de pH es el púrpura de bromocresol y en él se puede visualizar la producción de ácido sulhidrico.",
        CARD,
        0,
    ),
    (
        "Medio de cultivo en el cual el producto de la reacción del metabolismo bacteriano es la putrecina",
        CARD,
        0,
    ),
    (
        "Medio de cultivo en el cual se puede evaluar la utilización del triptófano por parte de los microorganismos ",
        CARD,
        0,
    ),
    ("Medio de cultivo semisólido", CARD, 0),
    (
        "Medio de cultivo en el cual una de las reacciones a observar, es la turbidez del medio.",
        CARD,
        0,
    ),
    (
        "Medio de cultivo en el cual se puede observar la descarboxilación de la ornitina. ",
        CARD,
        0,
    ),
    ("Medio de cultivo cuyo indicador de pH es el azul de bromotimol", CARD, 0),
    ("Medio de cultivo que se caracteriza por ser totalmente tendido", CARD, 0),
    ("Medio de cultivo que no requiere inoculación en profundidad.", CARD, 0),
    (
        "Medio de cultivo que contiene como indicador de pH el rojo fenol, y que posee pH final de 6,6 antes de ser inoculado",
        CARD,
        0,
    ),
    (
        "Medio de cultivo en el cual, al visualizar una reacción positiva con crecimiento bacteriano, se puede visualizar de color azul",
        CARD,
        0,
    ),
]  # 122 - 141

virusQuestions = [
    ("¿Qué tipo de microorganismos son los utilizados en terápia fágica?", CARD, 0),
    ("¿Qué tipo de microorganismo puede presntar simetría binaria?", CARD, 0),
    (
        "¿Qué tipos de virus que son inócuos para los seres humanos y que infectan solo a bacterias?",
        CARD,
        0,
    ),
    (
        "Son los únicos virus capaces de inyectar al citoplasma su material genético",
        CARD,
        0,
    ),
    (
        "Están compuestos por cabeza y cola. La cola contine receptores para la célula hospedera.",
        CARD,
        0,
    ),
    (
        "¿Qué tipo de virus presenta una cápside conformada por 20 caras triangulares y carece de envoltura externa?",
        CARD,
        0,
    ),
    (
        "¿Cómo se llama el virus cuya cápside está compuesta por una simetría icosaédrica y no tiene una cubierta lipídica externa?",
        CARD,
        0,
    ),
    (
        "¿Cuál es el nombre técnico para referirse a un virus que presenta una cápside formada por 20 triángulos idénticos y no está rodeado por una membrana?",
        CARD,
        0,
    ),
    (
        "¿Cuál es el tipo de virus en que sus espículas o glicoproptéinas están directamentte en la cápside?",
        CARD,
        0,
    ),
    (
        "¿Cuál es la estructura básica de un virus que consiste en una cápside formada por caras planas?",
        CARD,
        0,
    ),
    (
        "¿Cuál es el nombre del género de virus que incluye patógenos como el virus de la viruela y el virus del molusco contagioso?",
        CARD,
        0,
    ),
    (
        "¿Cómo se llama el grupo de virus que son grandes, de ADN de doble cadena y que causan enfermedades como la viruela y la peste bovina?",
        CARD,
        0,
    ),
    (
        "¿Qué virus, perteneciente a la familia Poxviridae, fue responsable de una de las enfermedades más devastadoras en la historia humana antes de ser erradicado?",
        CARD,
        0,
    ),
    (
        "¿Qué grupo de virus es conocido por su morfología única en forma de ladrillo y su capacidad para causar enfermedades graves en humanos y animales?",
        CARD,
        0,
    ),
    (
        "¿Qué virus, que tiene un genoma grande de ADN de doble cadena, es conocido por su capacidad para infectar una amplia gama de huéspedes, incluidos humanos y animales?",
        CARD,
        0,
    ),
    (
        "¿Cuál es el término que describe a un virus con un genoma dispuesto en una estructura helicoidal y envuelto en una membrana?",
        CARD,
        0,
    ),
    (
        "¿Qué tipo de virus tiene una estructura básica en forma de hélice y está cubierto por una capa de lípidos externa?",
        CARD,
        0,
    ),
    ("Tipo de morfología a la que pertenece el Virus del Ébola", CARD, 0),
    (
        "Morfología de los virus que más cómunmente producen fiebres hemorrágicas",
        CARD,
        0,
    ),
    (
        "¿Qué tipo de virus tiene una forma alargada con una envoltura externa y un genoma dispuesto en hélice?",
        CARD,
        0,
    ),
 ] #142 - 161

cocaceaTopic = [(i, 1) for i in range(1, 21)]
bacilosGramNTopic = [(i, 6) for i in range(21, 41)]
hongosTopic = [(i, 11) for i in range(41, 61)]
bacilosGramPTopic = [(i, 16) for i in range(61, 81)]
anaerobiosTopic = [(i, 21) for i in range(81, 102)]
mycobacTopic = [(i, 26) for i in range(102, 122)]
medioCultivoTopic = [(i, 36) for i in range(122, 142)]
virusTopic = [(i, 31) for i in range(142, 162)]


allTopics = [
    cocaceaTopic,
    bacilosGramNTopic,
    hongosTopic,
    bacilosGramPTopic,
    anaerobiosTopic,
    mycobacTopic,
    medioCultivoTopic,
]

cocaceaAnswers = [
    *[(i, 2) for i in range(1, 6)],
    *[(i, 3) for i in range(6, 11)],
    *[(i, 4) for i in range(11, 16)],
    *[(i, 5) for i in range(16, 21)],
]

bgnAnswers = [
    *[(i, 7) for i in range(21, 26)],
    *[(i, 8) for i in range(26, 31)],
    *[(i, 9) for i in range(31, 36)],
    *[(i, 10) for i in range(36, 41)],
]

hongosAnswers = [
    *[(i, 12) for i in range(41, 46)],
    *[(i, 13) for i in range(46, 51)],
    *[(i, 14) for i in range(51, 56)],
    *[(i, 15) for i in range(56, 61)],
]

bgpAnswers = [
    *[(i, 17) for i in range(61, 66)],
    *[(i, 18) for i in range(66, 71)],
    *[(i, 19) for i in range(71, 76)],
    *[(i, 20) for i in range(76, 81)],
]

anaerobiosAnswers = [
    *[(i, 22) for i in range(81, 86)],
    *[(i, 23) for i in range(86, 92)],
    *[(i, 24) for i in range(92, 97)],
    *[(i, 25) for i in range(97, 102)],
]

micobacAnswers = [
    *[(i, 27) for i in range(102, 107)],
    *[(i, 28) for i in range(107, 112)],
    *[(i, 29) for i in range(112, 117)],
    *[(i, 30) for i in range(117, 122)],
]

medioCultivoAnswers = [
    *[(i, 37) for i in range(122, 127)],
    *[(i, 38) for i in range(127, 132)],
    *[(i, 39) for i in range(132, 137)],
    *[(i, 40) for i in range(137, 142)],
]

virusAnswers = [
    *[(i, 32) for i in range(142, 147)],
    *[(i, 33) for i in range(147, 152)],
    *[(i, 34) for i in range(152, 157)],
    *[(i, 35) for i in range(157, 162)],
]

con = sqlite3.connect("../database_new.db")
cur = con.cursor()


def insertQuestions(questionSet):
    cur.executemany(
        "INSERT INTO Question(qQuestion, qType, qWrong) VALUES(?, ? ,?)",
        questionSet,
    )


def insertTopicQuestion(relation):
    cur.executemany("INSERT INTO QuestionTopic VALUES(?, ?)", relation)


def insertCardAnswer(relation):
    cur.executemany("INSERT INTO AnswerCard(qId, tId) VALUES(?, ?)", relation)


# for rel in allTopics:
insertQuestions(virusQuestions)
insertTopicQuestion(virusTopic)
insertCardAnswer(virusAnswers)
"""
for ans in [
    bgnAnswers,
    hongosAnswers,
    bgpAnswers,
    anaerobiosAnswers,
    micobacAnswers,
    medioCultivoAnswers,
]:
    insertCardAnswer(ans)
"""

con.commit()
