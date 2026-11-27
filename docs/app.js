const SCREENSHOTS = {
  en: "assets/screenshot-en.png",
  ca: "assets/screenshot-ca.png",
  es: "assets/screenshot-es.png"
};

const I18N = {
  en: {
    "nav.how": "How it works",
    "nav.features": "Features",
    "nav.download": "Download",
    "hero.eyebrow": "Open Source · macOS 13+ & Windows 10/11",
    "hero.title": "Your Apple Magic Keyboard, identical on macOS and Windows",
    "hero.subtitle": "Switch between Mac and PC over Bluetooth or USB with live battery levels, exact Apple keycaps, Option (⌥) symbols, and Command (⌘) shortcuts.",
    "cta.mac": "Download for macOS",
    "cta.win": "Download for Windows",
    "cta.source": "View source on GitHub →",
    "preview.zoomHint": "Tap to enlarge",
    "how.title": "Switch computers in seconds",
    "how.sub": "Magic Keyboard stores one active Bluetooth pairing at a time. Magic Keyboard Suite makes moving effortless.",
    "how.s1Title": "Release",
    "how.s1Body": "Click “Release” on your current computer. The app disconnects, forgets the pairing, and remembers your keyboard.",
    "how.s2Title": "Toggle switch",
    "how.s2Body": "Turn the keyboard's physical power switch off for 2 seconds and turn it back on.",
    "how.s3Title": "Connect",
    "how.s3Body": "Click “Connect” on your other computer from the main window, macOS menu bar, or Windows system tray.",
    "feat.title": "Built for Mac & Windows workflows",
    "feat.sub": "Everything you need, nothing you don't — scoped exclusively to Apple hardware.",
    "feat.1.tag": "Connectivity",
    "feat.1.title": "Bluetooth, USB & live battery",
    "feat.1.body": "Works wirelessly or over Lightning / USB-C, with real-time battery percentage in the app, macOS menu bar, and Windows tray.",
    "feat.2.tag": "Layouts",
    "feat.2.title": "17 Apple layouts & ISO fix",
    "feat.2.body": "Swaps the physical ISO keys on Windows automatically and imports Apple's 17 official Boot Camp layouts with Authenticode verification.",
    "feat.3.tag": "Shortcuts",
    "feat.3.title": "Mac shortcuts, Option symbols & media",
    "feat.3.body": "3 modifier modes, Left Option (⌥) for @ # € [ ] { } \\ | ~, word navigation, media keys, and optional Right Option as Fn on Windows.",
    "feat.4.tag": "Native",
    "feat.4.title": "SwiftUI & WinUI 3 · EN, CA & ES",
    "feat.4.body": "Two lightweight native apps built from a single shared design source, with live language switching and open-at-login support.",
    "dl.title": "Download",
    "dl.sub": "Ready-to-use installers from GitHub Releases, free and open source under GPLv3.",
    "dl.macTitle": "macOS Disk Image (.dmg)",
    "dl.macBody": "Native SwiftUI app with menu bar quick controls. Drag into Applications and you're ready.",
    "dl.macBtn": "Download .dmg",
    "dl.winTitle": "Windows Installer (.msi)",
    "dl.winBody": "Installs the WinUI 3 app, lightweight background tray agent, and optional launch at login.",
    "dl.winBtn": "Download .msi",
    "bc.tag": "Windows · Required for Apple layouts",
    "bc.title": "Import the 17 official Apple layouts (Boot Camp)",
    "bc.desc": "Download Apple's official Boot Camp Support Software .zip (do not unzip or run setup.exe) and pick it via “Select .zip…” inside Magic Keyboard Suite. The app extracts and verifies only the 17 keyboard layout DLLs.",
    "bc.btn": "Get Boot Camp .zip",
    "src.summary": "Build from source",
    "src.macTitle": "macOS (.app + .dmg)",
    "src.macReq": "Requires Xcode Command Line Tools & Python 3",
    "src.winTitle": "Windows 10/11 x64 (.msi)",
    "src.winReq": "Requires .NET 8 SDK, .NET 4.8 pack, WiX v5 & Python 3",
    "footer.disclaimer": "Independent open-source project under GPLv3. Not affiliated with or endorsed by Apple Inc. or Microsoft Corporation."
  },
  ca: {
    "nav.how": "Com funciona",
    "nav.features": "Característiques",
    "nav.download": "Descàrrega",
    "hero.eyebrow": "Codi Obert · macOS 13+ i Windows 10/11",
    "hero.title": "El teu Apple Magic Keyboard, idèntic a macOS i a Windows",
    "hero.subtitle": "Passa entre Mac i PC per Bluetooth o USB amb bateria en temps real, serigrafia exacta d'Apple, símbols amb Option (⌥) i dreceres amb Command (⌘).",
    "cta.mac": "Baixar per a macOS",
    "cta.win": "Baixar per a Windows",
    "cta.source": "Veure codi a GitHub →",
    "preview.zoomHint": "Toca per ampliar",
    "how.title": "Canvia d'ordinador en segons",
    "how.sub": "El Magic Keyboard només guarda un aparellament Bluetooth actiu. Magic Keyboard Suite fa que moure'l sigui molt fàcil.",
    "how.s1Title": "Allibera",
    "how.s1Body": "Prem «Alliberar» a l'ordinador actual. L'app el desconnecta, oblida l'aparellament i en recorda l'adreça.",
    "how.s2Title": "Interruptor físic",
    "how.s2Body": "Apaga l'interruptor físic del teclat durant 2 segons i torna'l a encendre.",
    "how.s3Title": "Connecta",
    "how.s3Body": "Prem «Connectar» a l'altre ordinador des de la finestra principal, la barra de menús de macOS o la safata de Windows.",
    "feat.title": "Pensat per treballar entre Mac i Windows",
    "feat.sub": "Tot el que necessites i res que sobri, aplicat exclusivament a maquinari d'Apple.",
    "feat.1.tag": "Connectivitat",
    "feat.1.title": "Bluetooth, USB i bateria en viu",
    "feat.1.body": "Funciona sense fils o per Lightning / USB-C, amb el percentatge de bateria en temps real a l'app, la barra de menús i la safata de Windows.",
    "feat.2.tag": "Disposicions",
    "feat.2.title": "17 disposicions Apple i correcció ISO",
    "feat.2.body": "Intercanvia les tecles físiques ISO automàticament a Windows i importa les 17 disposicions de Boot Camp verificades amb Authenticode.",
    "feat.3.tag": "Dreceres",
    "feat.3.title": "Dreceres Mac, símbols Option i multimèdia",
    "feat.3.body": "3 modes de modificadors, Option esquerra per a @ # € [ ] { } \\ | ~, navegació per paraules, tecles multimèdia i Option dret com a Fn.",
    "feat.4.tag": "Natiu",
    "feat.4.title": "SwiftUI i WinUI 3 · CA, ES i EN",
    "feat.4.body": "Dues apps natives lleugeres creades des d'una única font de disseny, amb canvi d'idioma en viu i inici automàtic.",
    "dl.title": "Descàrrega",
    "dl.sub": "Instal·ladors a punt per utilitzar des de GitHub Releases, gratuïts i de codi obert sota GPLv3.",
    "dl.macTitle": "Imatge de disc macOS (.dmg)",
    "dl.macBody": "App nativa SwiftUI amb accés ràpid a la barra de menús. Arrossega-la a Aplicacions i llest.",
    "dl.macBtn": "Baixar .dmg",
    "dl.winTitle": "Instal·lador Windows (.msi)",
    "dl.winBody": "Instal·la l'app WinUI 3, l'agent lleuger a la safata del sistema i l'inici automàtic opcional.",
    "dl.winBtn": "Baixar .msi",
    "bc.tag": "Windows · Necessari per a les disposicions Apple",
    "bc.title": "Importa les 17 disposicions oficials d'Apple (Boot Camp)",
    "bc.desc": "Baixa el .zip oficial de Boot Camp Support Software d'Apple (no el descomprimeixis ni executis setup.exe) i tria'l amb «Triar .zip…» dins l'app. Només s'extrauran i verificaran les 17 llibreries de teclat.",
    "bc.btn": "Baixar Boot Camp .zip",
    "src.summary": "Compilar des del codi font",
    "src.macTitle": "macOS (.app + .dmg)",
    "src.macReq": "Requereix Xcode Command Line Tools i Python 3",
    "src.winTitle": "Windows 10/11 x64 (.msi)",
    "src.winReq": "Requereix .NET 8 SDK, paquet .NET 4.8, WiX v5 i Python 3",
    "footer.disclaimer": "Projecte independent de codi obert sota llicència GPLv3. No afiliat ni avalat per Apple Inc. ni Microsoft Corporation."
  },
  es: {
    "nav.how": "Cómo funciona",
    "nav.features": "Características",
    "nav.download": "Descarga",
    "hero.eyebrow": "Código Abierto · macOS 13+ y Windows 10/11",
    "hero.title": "Tu Apple Magic Keyboard, idéntico en macOS y Windows",
    "hero.subtitle": "Cambia entre Mac y PC por Bluetooth o USB con batería en tiempo real, serigrafía exacta de Apple, símbolos con Option (⌥) y atajos con Command (⌘).",
    "cta.mac": "Descargar para macOS",
    "cta.win": "Descargar para Windows",
    "cta.source": "Ver código en GitHub →",
    "preview.zoomHint": "Toca para ampliar",
    "how.title": "Cambia de ordenador en segundos",
    "how.sub": "Magic Keyboard solo guarda un emparejamiento Bluetooth activo. Magic Keyboard Suite hace que cambiar sea rapidísimo.",
    "how.s1Title": "Libera",
    "how.s1Body": "Pulsa «Liberar» en tu equipo actual. La app lo desconecta, olvida el emparejamiento y recuerda tu teclado.",
    "how.s2Title": "Interruptor físico",
    "how.s2Body": "Apaga el interruptor físico del teclado durante 2 segundos y vuélvelo a encender.",
    "how.s3Title": "Conecta",
    "how.s3Body": "Pulsa «Conectar» en el otro equipo desde la ventana principal, la barra de menús de macOS o la bandeja de Windows.",
    "feat.title": "Pensado para trabajar entre Mac y Windows",
    "feat.sub": "Todo lo que necesitas y nada que sobre, aplicado exclusivamente a hardware de Apple.",
    "feat.1.tag": "Conectividad",
    "feat.1.title": "Bluetooth, USB y batería en vivo",
    "feat.1.body": "Funciona sin cables o por Lightning / USB-C, con porcentaje de batería en tiempo real en la app, la barra de menús y la bandeja de Windows.",
    "feat.2.tag": "Disposiciones",
    "feat.2.title": "17 disposiciones Apple y corrección ISO",
    "feat.2.body": "Intercambia las teclas físicas ISO automáticamente en Windows e importa las 17 disposiciones de Boot Camp verificadas con Authenticode.",
    "feat.3.tag": "Atajos",
    "feat.3.title": "Atajos Mac, símbolos Option y multimedia",
    "feat.3.body": "3 modos de modificadores, Option izquierda para @ # € [ ] { } \\ | ~, navegación por palabras, teclas multimedia y Option derecho como Fn.",
    "feat.4.tag": "Nativo",
    "feat.4.title": "SwiftUI y WinUI 3 · ES, CA y EN",
    "feat.4.body": "Dos apps nativas ligeras creadas desde una única fuente de diseño, con cambio de idioma en vivo e inicio automático.",
    "dl.title": "Descarga",
    "dl.sub": "Instaladores listos para usar desde GitHub Releases, gratuitos y de código abierto bajo GPLv3.",
    "dl.macTitle": "Imagen de disco macOS (.dmg)",
    "dl.macBody": "App nativa SwiftUI con acceso rápido en la barra de menús. Arrástrala a Aplicaciones y listo.",
    "dl.macBtn": "Descargar .dmg",
    "dl.winTitle": "Instalador Windows (.msi)",
    "dl.winBody": "Instala la app WinUI 3, el agente ligero en la bandeja del sistema y el arranque automático opcional.",
    "dl.winBtn": "Descargar .msi",
    "bc.tag": "Windows · Necesario para las disposiciones Apple",
    "bc.title": "Importa las 17 disposiciones oficiales de Apple (Boot Camp)",
    "bc.desc": "Descarga el .zip oficial de Boot Camp Support Software de Apple (no lo descomprimas ni ejecutes setup.exe) y selecciónalo con «Elegir .zip…» dentro de la app. Solo se extraerán y verificarán las 17 librerías de teclado.",
    "bc.btn": "Descargar Boot Camp .zip",
    "src.summary": "Compilar desde el código fuente",
    "src.macTitle": "macOS (.app + .dmg)",
    "src.macReq": "Requiere Xcode Command Line Tools y Python 3",
    "src.winTitle": "Windows 10/11 x64 (.msi)",
    "src.winReq": "Requiere .NET 8 SDK, paquete .NET 4.8, WiX v5 y Python 3",
    "footer.disclaimer": "Proyecto independiente de código abierto bajo licencia GPLv3. No afiliado ni respaldado por Apple Inc. ni Microsoft Corporation."
  }
};

function setLanguage(lang) {
  const activeLang = I18N[lang] ? lang : "en";
  const dict = I18N[activeLang];
  document.documentElement.lang = activeLang;
  try { localStorage.setItem("mks_lang", activeLang); } catch {}

  document.querySelectorAll("[data-i18n]").forEach((el) => {
    const key = el.getAttribute("data-i18n");
    if (dict[key]) {
      el.textContent = dict[key];
    }
  });

  document.querySelectorAll(".lang-btn").forEach((btn) => {
    btn.classList.toggle("active", btn.getAttribute("data-lang") === activeLang);
  });

  const src = SCREENSHOTS[activeLang] || SCREENSHOTS.en;
  const img = document.getElementById("app-screenshot");
  const lbImg = document.getElementById("lightbox-img");
  if (img) img.src = src;
  if (lbImg) lbImg.src = src;
}

function initRepoLinks() {
  let repoUrl = "https://github.com/Marstwan/Magic-Keyboard-Suite";
  const host = window.location.hostname;
  const pathParts = window.location.pathname.split("/").filter(Boolean);
  if (host.endsWith(".github.io") && pathParts.length >= 1) {
    const owner = host.replace(".github.io", "");
    const repo = pathParts[0];
    repoUrl = `https://github.com/${owner}/${repo}`;
  }

  const dmgUrl = `${repoUrl}/releases/latest/download/MagicKeyboardSuite-macOS.dmg`;
  const msiUrl = `${repoUrl}/releases/latest/download/MagicKeyboardSuite-Setup.msi`;

  const setHref = (id, url) => {
    const el = document.getElementById(id);
    if (el) el.href = url;
  };

  setHref("download-mac", dmgUrl);
  setHref("download-win", msiUrl);
  setHref("release-mac-btn", dmgUrl);
  setHref("release-win-btn", msiUrl);
  setHref("view-github", repoUrl);
  setHref("footer-github", repoUrl);
  setHref("footer-releases", `${repoUrl}/releases`);
  setHref("footer-license", `${repoUrl}/blob/main/LICENSE`);

  const ua = navigator.userAgent.toLowerCase();
  const btnMac = document.getElementById("download-mac");
  const btnWin = document.getElementById("download-win");
  if (ua.includes("win") && btnWin && btnMac) {
    btnWin.classList.add("btn-primary");
    btnMac.classList.remove("btn-primary");
  }
}

function initLightbox() {
  const trigger = document.getElementById("screenshot-trigger");
  const lightbox = document.getElementById("lightbox");
  const closeBtn = document.getElementById("lightbox-close");
  if (!trigger || !lightbox) return;

  const open = () => {
    lightbox.classList.add("open");
    lightbox.setAttribute("aria-hidden", "false");
    document.body.style.overflow = "hidden";
  };

  const close = () => {
    lightbox.classList.remove("open");
    lightbox.setAttribute("aria-hidden", "true");
    document.body.style.overflow = "";
  };

  trigger.addEventListener("click", open);
  lightbox.addEventListener("click", close);
  if (closeBtn) closeBtn.addEventListener("click", close);
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && lightbox.classList.contains("open")) {
      close();
    }
  });
}

function initSpatialMotion() {
  const prefersReduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const isTouch = window.matchMedia("(hover: none), (pointer: coarse)").matches;
  if (prefersReduced) return;

  const frame = document.getElementById("screenshot-trigger");
  if (frame && !isTouch) {
    frame.addEventListener("mousemove", (e) => {
      const rect = frame.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width - 0.5;
      const y = (e.clientY - rect.top) / rect.height - 0.5;
      frame.style.transform = `rotateX(${(-y * 5).toFixed(2)}deg) rotateY(${(x * 5).toFixed(2)}deg)`;
    });
    frame.addEventListener("mouseleave", () => {
      frame.style.transform = "rotateX(0deg) rotateY(0deg)";
    });
  }

  if (typeof window.gsap === "undefined" || typeof window.ScrollTrigger === "undefined") {
    return;
  }

  const gsap = window.gsap;
  gsap.registerPlugin(window.ScrollTrigger);

  gsap.from(".hero-eyebrow, .hero h1, .hero-sub, .cta-row, .hero-meta-link", {
    y: 18,
    opacity: 0,
    duration: 0.65,
    stagger: 0.08,
    ease: "power3.out",
    clearProps: "transform,opacity"
  });

  gsap.from("#screenshot-trigger", {
    y: 26,
    opacity: 0,
    duration: 0.75,
    delay: 0.18,
    ease: "power3.out",
    clearProps: "transform,opacity"
  });

  document.querySelectorAll(".stagger-group").forEach((group) => {
    const items = group.querySelectorAll(".stagger-item");
    if (!items.length) return;
    gsap.from(items, {
      y: 24,
      opacity: 0,
      duration: 0.6,
      stagger: 0.09,
      ease: "power3.out",
      clearProps: "transform,opacity",
      scrollTrigger: {
        trigger: group,
        start: "top 86%",
        once: true
      }
    });
  });

  if (!isTouch) {
    gsap.to(".orb-1", {
      y: 80,
      ease: "none",
      scrollTrigger: { trigger: "body", start: "top top", end: "bottom bottom", scrub: 1 }
    });
    gsap.to(".orb-2", {
      y: -70,
      ease: "none",
      scrollTrigger: { trigger: "body", start: "top top", end: "bottom bottom", scrub: 1 }
    });
  }
}

document.addEventListener("DOMContentLoaded", () => {
  let saved = null;
  try { saved = localStorage.getItem("mks_lang"); } catch {}
  const browserLang = (navigator.language || "en").slice(0, 2).toLowerCase();
  const initialLang = saved || (I18N[browserLang] ? browserLang : "en");
  setLanguage(initialLang);
  initRepoLinks();
  initLightbox();

  document.querySelectorAll(".lang-btn").forEach((btn) => {
    btn.addEventListener("click", () => setLanguage(btn.getAttribute("data-lang")));
  });

  initSpatialMotion();
});
