# Enxada & Encrenca (Hoe & Behold)

Simulador de fazenda 2D top-down em pixel art, estilo Stardew Valley, na roça brasileira.
O jogador herda o sítio abandonado da avó em **Vila Chuchu** e reergue o lugar.
Tom: humor leve e caloroso (galinha brava, vizinho encrenqueiro, causos, quermesse). Sem violência pesada.

O diretor do jogo (usuário) decide o design, testa no editor e aprova cada etapa.
Fale com ele em **português**. Trabalhe em etapas pequenas e espere o ok antes da próxima.

## Pilares (todo sistema serve a pelo menos um)
1. Recomeço visível: o sítio melhora no mapa.
2. Tempo que passa: dias, 4 estações de 28 dias, festivais.
3. Produção e economia: plantar, colher, vender, melhorar.
4. Comunidade: moradores com rotina, gostos e amizade.
5. Liberdade sem punição: sem game over; desmaiar custa só dinheiro e tempo.

MVP = uma Primavera jogável (28 dias), sítio + vila com 3 moradores (Dona Cida, Seu Juca, Tonho) e a Venda da Dona Cida.
Pós-MVP (só deixar a arquitetura preparada): pesca, mina, animais, cozinha, upgrades de ferramentas, festa junina, Quermesse, casamento, coop.

## Regra de ouro: só ferramentas gratuitas
- Unity 6 LTS, licença Personal. Pacotes oficiais gratuitos (ver `Packages/manifest.json`).
- Arte/som: placeholders gerados por código ou assets **CC0** (ex.: Kenney). Fontes **OFL/CC0** com acentos.
- Antes de usar asset de terceiros: confirme a licença e registre em `Assets/_Project/Art/CREDITS.md`.
- Nunca instale nada pago nem peça para o usuário comprar algo.

## Convenções de código
- Código em **inglês**; comentários e mensagens para o diretor em **português**.
- Texto visível ao jogador vai para tabelas de **Localization** (PT-BR já a partir da Etapa 2; EN na Etapa 10). Nunca hardcoded.
- Namespaces / assemblies (um `.asmdef` por módulo em `Assets/_Project/Scripts/<Módulo>/`):
  `Enxada.Core`, `Enxada.Calendar`, `Enxada.Farming`, `Enxada.Inventory`, `Enxada.Player`,
  `Enxada.NPC`, `Enxada.UI`, `Enxada.Save`, `Enxada.EditorTools`, `Enxada.Tests.EditMode`, `Enxada.Tests.PlayMode`.
  - **Não** usar `Enxada.Time` (colide com `UnityEngine.Time`) nem `Enxada.Editor` (colide com `UnityEditor.Editor`).
  - Classe principal não tem o mesmo nome do namespace: `InventoryModel`, `PlayerController`.
- Comunicação entre sistemas: eventos C# ou `EventChannel` (ScriptableObject). Sem referências diretas entre módulos.
  Serviços globais são registrados no `ServiceLocator` pelo `GameBootstrap` (cena Boot). Evite singletons.
- Dados fora do código: itens, sementes, ferramentas, moradores, lojas, diálogos e balanceamento em ScriptableObjects
  (`Assets/_Project/Data/`). Nada de números mágicos.
- Desempenho: nada de `Find`/`GetComponent` em `Update`, nem alocação por frame. Pooling para itens no chão e efeitos.
- Grade: tiles 16×16 px, Pixel Perfect Camera com referência 320×180 (ortho size 5,625), escala para 1920×1080.

### Lógica pura e testes
- Lógica testável (relógio, crescimento, inventário, economia, amizade, save) fica em pastas
  `Assets/_Project/Scripts/<Módulo>/Logic/` **sem `using UnityEngine`**. MonoBehaviours só fazem a ponte.
- Testes dessa lógica ficam em `Assets/_Project/Tests/EditMode/Logic/` (também sem UnityEngine).
- Testes que precisam da Unity ficam em `Tests/EditMode/<Módulo>/` ou `Tests/PlayMode/`.
- **Harness .NET** (`Tools/LogicTests/`): compila a lógica como netstandard2.1 + C# 9 (mesmo perfil da Unity) e roda
  os testes com NUnit 3. Rode antes de todo push: `Tools/LogicTests/run.sh` (requer .NET 8 SDK).
  O Claude trabalha numa nuvem sem Unity: esse harness é a única verificação de compilação antes do diretor abrir o editor.

### Cenas, prefabs e .meta
- **Nunca editar `.unity`/`.prefab` à mão.** Cenas, prefabs, tilemaps e SOs são montados por scripts de editor
  no menu `Enxada/Setup/...` (código em `Assets/_Project/Editor/`). Os passos devem ser idempotentes.
- Sempre commitar os `.meta`. Arquivos criados fora da Unity: rode `python3 Tools/gen_meta.py` para gerar o `.meta`
  (GUID estável). Tipos com importer complexo (`.inputactions`, `.png`...) a Unity gera e o diretor commita.
- Pastas vazias levam um `.gitkeep` (a Unity ignora arquivos ocultos).
- Fluxo de cenas: `Boot` → `MainMenu` → `Farm` → `Town` (→ `Mine` pós-MVP), com fade, mantendo jogador e estado.

### Git
- `.gitignore` oficial de Unity; Git LFS para `png/psd/aseprite/wav/ogg/mp3/ttf/otf`.
- Um commit por etapa (ou sub-passo), mensagem em português.

## Decisões de design aprovadas (Etapa 0)
- Configurações (volume, idioma, teclas) ficam em `settings.json` global, separado dos 3 slots de save.
- Dormir dispara `OnDayEnded`; o save se pluga nele na Etapa 7.
- Moradores: posição calculada pela agenda + hora quando a cena carrega; A* só na cena ativa.
- Presentes amados viram itens vendidos na Venda da Dona Cida no MVP (café, bolo de milho, garapa, pimenta);
  peixe frito e receitas só no pós-MVP.
- Tonho no MVP reclama do mato e da bagunça do sítio; galinhas entram com os animais (pós-MVP).
- Clima simples (sol/chuva por chance configurável) na Etapa 5; previsão na TV na Etapa 11.
- Etapa 8 dividida em 8a (diálogo + amizade + presentes) e 8b (agenda + pathfinding).
- Crescimento: semente plantada no dia 1 com "4 dias" fica pronta no dia 5 (regada todos os dias).
- Fim do MVP: ao dormir no dia 28 da Primavera aparece uma **tela de fim da demo** (resumo da estação).
  O dia 28→Verão não é jogável no MVP; a lógica do calendário continua genérica para as 4 estações.

## Status do roadmap
| Etapa | Entrega | Status |
| --- | --- | --- |
| 0 | Setup: CLAUDE.md, git, pacotes, pastas, asmdefs, cena Boot, menu `Enxada/Setup` | ✅ Código pronto; aguardando validação no editor |
| 1 | Jogador, Input System, câmera Cinemachine, mapa de teste com colisão | ✅ Código pronto; aguardando validação no editor |
| 2 | Relógio, calendário, luz do dia, dormir | ⏳ |
| 3 | Inventário e barra rápida | ⏳ |
| 4 | Ferramentas e energia | ⏳ |
| 5 | Plantio, colheita e clima simples | ⏳ |
| 6 | Baú de entregas, dinheiro e Venda da Dona Cida | ⏳ |
| 7 | Save/Load e menu principal | ⏳ |
| 8a | Diálogos, amizade e presentes | ⏳ |
| 8b | Agenda dos moradores e pathfinding | ⏳ |
| 9 | Vila Chuchu e transição de cenas | ⏳ |
| 10 | Localização PT/EN, opções, áudio | ⏳ |
| 11 | Polimento e balanceamento da Primavera | ⏳ |

### Notas técnicas abertas
- Versões dos pacotes fixadas para Unity **6000.0 LTS**, de memória (o registro da Unity não é acessível da nuvem).
  Se o editor do diretor for outro (ex.: 6000.3), ajustar o `manifest.json` conforme o Package Manager.
- Input System: ao abrir o projeto a Unity pergunta se ativa o novo backend de input; responder **Yes**.
- `EnxadaControls.inputactions`: mapas `Gameplay` e `UI`, esquemas Teclado&Mouse e Gamepad, barra rápida 1–0, `-`, `=`.

### Etapa 1: o que existe
- `Player/Logic`: `MovementInput` (4/8 direções, deadzone, analógico) e `TileTargeting` (tile à frente / sob o mouse com alcance). 28 testes no harness.
- `PlayerController` (Rigidbody2D, lê `Gameplay/Move`), `TargetTileSelector` (`TargetCell` + evento `TargetChanged` para as próximas etapas), `PlayerConfig` (SO).
- Menu `Enxada/Setup/Criar Mapa de Teste`: gera sprites/tiles placeholder, `PlayerConfig`, prefab `Player`, e a cena `Scenes/Dev/TestMap`
  (Grid + Ground/Obstacles com colisão composta, Cinemachine com confiner, Pixel Perfect 320x180, Global Light 2D).
  A cena TestMap é **regenerada a cada execução**; assets só são criados se não existirem (apague o prefab/tiles para recriar).
- `Light2D` e `PixelPerfectCamera` são buscados por nome (assembly varia entre versões do URP). Cinemachine usa a API tipada 3.x.
- Pendente de etapas futuras: animação andando/ferramenta (hoje só 1 sprite por direção), Rule Tiles, tela de rebinding (Etapa 10).
