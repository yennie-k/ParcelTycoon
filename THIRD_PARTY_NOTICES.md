# 외부 리소스 고지

확인일: 2026-11-15. 이 문서는 포함된 외부 리소스의 출처·적용 조건을 구분합니다. 저장소 전체에 단일 라이선스를 부여하지 않으며, 확인되지 않은 권리를 새로 부여하지 않습니다. 심사 기간에만 공개하더라도 각 리소스의 배포 조건을 준수해야 합니다.

## 1. Noto Sans KR — SIL Open Font License 1.1

Copyright 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'.

This Font Software is licensed under the SIL Open Font License, Version 1.1.

- 원본: `Assets/_Project/Fonts/NotoSansKR-VariableFont_wght.ttf`
- 생성된 폰트 리소스: `Assets/_Project/Fonts/NotoSansKR Goal SDF.asset` 및 그 안에 포함된 글리프/아틀라스 데이터
- 저작권·라이선스 전문: [NotoSansKR-OFL.txt](Licenses/NotoSansKR-OFL.txt)
- 공식 고지 출처: https://raw.githubusercontent.com/google/fonts/main/ofl/notosanskr/OFL.txt
- 내부 TTF name 테이블에서 Adobe 2014–2021, 버전 `2.004-H2`, OFL 1.1 고지와 일치함을 확인했습니다. 프로젝트 폰트 전체가 upstream의 현재 바이너리와 동일하다고 주장하지는 않습니다.

OFL 고지와 전문을 함께 보존해야 합니다. 폰트 자체를 단독 판매하거나 다른 라이선스로 재허가하지 않습니다. 생성된 SDF/아틀라스에도 원본 폰트의 조건을 유지하며, 예약 명칭 조건을 준수해야 합니다.

## 2. Liberation Sans — SIL Open Font License 1.1

Digitized data copyright (c) 2010 Google Corporation, with Reserved Font Arimo, Tinos and Cousine.
Copyright (c) 2012 Red Hat, Inc., with Reserved Font Name Liberation.

- 폰트: `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`
- 관련 리소스: `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans*`
- 기존 저작권·OFL 전문: [LiberationSans - OFL.txt](<Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>)

기존 고지를 변경하거나 삭제하지 않았습니다. 해당 폰트와 파생 폰트 데이터에는 OFL이 적용됩니다.

## 3. Unity uGUI / TextMesh Pro 리소스

로컬 패키지 `com.unity.ugui 2.0.0`은 TMP를 포함하며 패키지 `LICENSE.md`는 Unity Companion License를 지정합니다.

- 원문 패키지 고지: [Unity-uGUI-LICENSE.md](Licenses/Unity-uGUI-LICENSE.md)
- 공식 라이선스: https://unity.com/legal/licenses/unity-companion-license
- 공식 페이지의 법률 본문을 텍스트로 보관: [Unity-Companion-License-1.4.txt](Licenses/Unity-Companion-License-1.4.txt), v1.4 / 10.29.2024
- 고지 대상: `Assets/TextMesh Pro/Shaders`, 기본 TMP 설정·스타일 등 패키지에서 제공된 Unity 소유 리소스

설치된 패키지의 `Package Resources/TMP Essential Resources.unitypackage`에서 EmojiOne PNG를 확인했고, 현재 PNG와 SHA-256이 일치했습니다. 다만 폰트와 EmojiOne 등 제3자 저작물은 위 패키지 라이선스로 일괄 허가되는 것이 아니며 각각의 조건을 확인해야 합니다.

이 프로젝트의 Unity 관련 사용·배포에는 Unity Companion License의 조건과 고지 보존 의무가 적용됩니다. 엔진 자체를 이 저장소에 포함하거나 재허가하지 않습니다. 다른 UPM 패키지는 `Packages`의 manifest/lock으로 복원되며 각 패키지의 고유 라이선스를 따릅니다.

## 4. Unity URP 템플릿

- 원문 고지: [Unity-Template-LICENSE.md](Licenses/Unity-Template-LICENSE.md)
- 고지에는 `com.unity.template.urp-blank`, Unity Technologies ApS, 2021 및 Unity Companion License가 명시되어 있습니다.
- 확인 출처: 설치된 Unity 6000.3.10f1의 `com.unity.template.3d-cross-platform-17.0.14.tgz` 내부 `package/LICENSE.md`
- `Assets/Readme.asset`, `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`, `Assets/TutorialInfo/Icons/URP.png`, `Assets/TutorialInfo/Layout.wlt`는 해당 공식 템플릿 파일과 바이트 단위로 일치합니다.

템플릿의 저작권 고지를 함께 보존합니다. 위 Unity Companion License 전문을 참조하세요.

## 5. EmojiOne — 현재 파일에 적용할 정확한 버전·조건 확인 필요

### 포함된 파일과 연결 관계

- `Assets/TextMesh Pro/Sprites/EmojiOne.png` — 512×512 아틀라스, 128×128 타일 16개
- `Assets/TextMesh Pro/Sprites/EmojiOne.json` — 타일 배치 정보
- [기존 EmojiOne Attribution.txt](<Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt>)
- `Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset`

Artwork provided by EmojiOne: https://www.emojione.com/

PNG SHA-256: `a87b965a701de3dc31d7401e542b1fc946587f912c51d7a26e46ade9f098bd1a`

설치된 TMP Essential Resources의 PNG와 바이트 단위로 일치합니다. `TMP Settings.asset`의 기본 Sprite Asset은 EmojiOne을 참조하므로 **프로젝트에 포함되며 기본 설정에도 연결된 리소스**입니다. 현재 게임 코드·씬에서 `<sprite>` 표시 구문이나 별도 EmojiOne 직접 참조는 발견하지 못했습니다. 이는 화면에 직접 표시된 사용 흔적을 찾지 못했다는 의미이며, 빌드 제외나 재배포 허가를 의미하지 않습니다.

### 공식 버전별 조건과 남은 확인

- 공식 v2.2.7 고지: https://github.com/joypixels/emojione/blob/v2.2.7/LICENSE.md
- 공식 v2.2.7 설명: https://github.com/joypixels/emojione/blob/v2.2.7/README.md#licenses
- 이 버전은 PNG/SVG 및 변형 이미지에 **CC BY 4.0**, 비이미지 코드 등에 MIT를 명시합니다. 구버전(<2.0.0)에는 **CC BY-SA 4.0**을 구분해 안내합니다.
- 비교용으로 받은 v2.2.7의 해당 개별 PNG 16개는 64×64로, 현재 128×128 타일과 해상도가 다릅니다. 현재 아틀라스의 정확한 원본 버전을 확정하거나 v2.2.7과 픽셀 일치를 입증하지는 못했습니다.

따라서 현재 PNG에 CC BY 4.0 또는 다른 라이선스를 임의 지정하지 않습니다. 현재 JSON도 Unity에서 생성한 아틀라스 배치 파일이므로 upstream 코드의 MIT 라이선스를 이름만 보고 적용하지 않습니다.

**공개 전 남은 조치:** TMP에 포함된 해당 이미지의 원본 버전과 허가 근거를 확인한 뒤 정확한 저작권·라이선스·변경 내역 고지를 완성해야 합니다. 확인이 어려우면 소유자가 기본 Sprite 연결과 사용 범위를 검토한 후 제거 또는 교체를 별도 승인하는 방법이 있습니다. 이번 작업에서는 관련 파일·TMP 설정을 변경하거나 삭제하지 않았습니다.
