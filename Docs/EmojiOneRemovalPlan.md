# EmojiOne 제거 결과와 Git 이력 정리 제안

이 문서의 Git 이력 작업은 **제안이며 실행하지 않았습니다.** 현재 프로젝트 제거와 과거 이력 삭제는 별개의 작업입니다. 기존 GitHub 저장소 `yennie-k/ParcelTycoon`을 유지합니다.

## 현재 프로젝트 변경

`Assets/TextMesh Pro/Resources/TMP Settings.asset`의 `m_defaultSpriteAsset`을 `{fileID: 0}`으로 변경했습니다. 한국어 폰트·셰이더·스타일·Emoji Support 등의 다른 설정은 변경하지 않았습니다.

삭제한 정확한 경로는 다음 8개입니다. 이 목록은 이후 승인된 이력 정리의 경로 목록으로도 사용할 수 있습니다.

```text
Assets/TextMesh Pro/Sprites/EmojiOne.png
Assets/TextMesh Pro/Sprites/EmojiOne.png.meta
Assets/TextMesh Pro/Sprites/EmojiOne.json
Assets/TextMesh Pro/Sprites/EmojiOne.json.meta
Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt
Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt.meta
Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset
Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset.meta
```

폴더의 `.meta`, Noto Sans KR, Liberation Sans, 기존 TMP 셰이더와 기타 외부 리소스는 보존합니다. 재임포트 과정에서 TMP Essential Resources를 다시 가져오면 EmojiOne이 재생성될 수 있으므로 재검사가 필요합니다.

## 검증 결과와 범위

- 삭제한 GUID의 외부 참조는 제거 전 기본 Sprite 설정 한 곳이었으며, 제거 후 Unity 에셋의 잔존 참조가 없는지 검사합니다.
- 실제 프로젝트의 게임 코드·씬·프리팹·폰트·재질·Packages·ProjectSettings는 변경하지 않습니다. 원본 대비 해시로 검사합니다.
- Unity 6000.3.10f1 격리 복사본의 실제 Play Mode에서 신규 게임 시작과 기존 저장 복사본 복원을 검사했습니다. 재화와 구매·부분 결제·업그레이드 8개 기록이 기존 저장과 일치했습니다.
- SaveManager의 저장 경로는 **검증용 복사본에서만** Library 안으로 변경했습니다. 실제 저장 파일에는 쓰지 않았습니다. 검사 직후 게임 로직을 정지시켜 동일한 상태로 비교했습니다.
- 동일한 초기화·렌더 예열 조건에서 신규 게임·저장 복원 각각의 제거 전후 1920×1080 렌더는 2,073,600픽셀 모두 동일했습니다. 초기 예열 전 비교는 재질 초기화 차이로 실패해 비교 기준에서 제외하고 다시 검사했습니다.
- MainScene의 직렬화 참조 469개와 Missing Script 검사를 통과했습니다. 한국어 Goal·진행도 12개 상태, 패드 8개의 TMP 메시 60개, Input 1~10 적재, INPUT/OUTPUT 갱신, Save v3 JSON·레거시 구매 상태, 저장 초기화 격리 11개 사례, 경제 검사 99개가 통과했습니다.
- 이 결과는 통제된 Play Mode·렌더·회귀 검증입니다. 사용자가 직접 조작하는 장시간 플레이, Android 실기기 빌드·터치 입력, 모든 구버전 실제 저장의 복원까지 새로 확인한 것은 아닙니다.
- 검증 도구·로그·비교 PNG와 제거 파일의 로컬 백업은 Git에서 제외되는 `Library/EmojiRemovalAudit` 및 기존 격리 프로젝트에만 보관합니다. 이 백업은 저장소 전체의 안전 백업을 대신하지 않습니다.

## 이력 정리 전제

요청에서 언급한 21개에 포트폴리오 문서 커밋이 추가되어, 조사 시 main에는 22개 커밋이 있습니다. EmojiOne 추가 커밋은 `d65ecc4`입니다. 경로 필터링만으로도 그 커밋과 후속 커밋의 해시는 바뀌므로 **기존 SHA를 유지하면서 과거 파일만 없애는 것은 불가능**합니다. 파일만 포함한 커밋이 비면 기본 동작에 따라 커밋 수가 달라질 수도 있습니다.

`git filter-repo --version`은 현재 사용할 수 없었습니다. 설치·업그레이드는 하지 않았습니다. Git과 Python은 설치되어 있으며, 승인 후 공식 설치 안내에 따라 도구를 준비하고 버전을 확인할 수 있습니다.

공식 설치 안내: https://github.com/newren/git-filter-repo/blob/main/INSTALL.md

## 별도 승인 후 권장 순서

1. **변경 동결과 백업:** 다른 컴퓨터의 미커밋 작업도 별도로 보관하고 Push를 중단합니다. 현재 작업 트리·`.git`·미추적 파일을 비공개 저장 장치에 복사하고, `git bundle create <비공개백업경로>/ParcelTycoon-before.bundle --all` 및 `git bundle verify`로 Git 백업을 검증합니다. Bundle은 작업 트리의 미커밋 삭제·수정·미추적 파일을 보존하지 않으므로 파일 백업도 필요합니다. 백업에는 EmojiOne 원본이 있으므로 공개 저장소에 넣지 않습니다.
2. **원격 상태 기록:** 기존 저장소의 Private 상태, main SHA, 모든 원격 브랜치·태그, PR·포크·릴리스 등을 조사합니다. 로컬 `refs/codex/*` 등 사용자 브랜치 외 참조도 구분합니다. 이번에는 원격 상태를 바꾸지 않았습니다.
3. **로컬 전용 정리 복사본:** 기존 원격에서 새 로컬 mirror clone을 만들고, 검증한 경로 8개를 대상으로 필터링합니다. 이는 새 GitHub 저장소 생성이 아닙니다. 실작업 원본에서 `--force`로 도구의 fresh-clone 검사를 우회하지 않습니다.
4. **필터 제안:** 위 8줄을 외부 경로 목록 파일로 두고 `git filter-repo --invert-paths --paths-from-file <경로목록파일>`을 사용합니다. 과거 이름 변경·다른 경로·동일 바이너리 복제본이 발견되면 그 경로도 추가합니다. 이번 조회에서 알려진 추가 경로는 없었지만 실행 직전 다시 검사해야 합니다.
5. **과거 TMP 설정 주의:** 파일 경로만 제거하면 과거 커밋의 TMP Settings에는 삭제된 Sprite GUID가 남을 수 있습니다. 최신 트리는 이번 None 설정으로 해결되지만, 모든 과거 커밋까지 실행 가능한 상태로 유지하려면 과거 TMP 설정의 해당 참조도 바꾸는 별도 필터가 필요합니다. 이 추가 내용 변경은 실행 전에 범위를 따로 확정해야 합니다.
6. **로컬 검증:** 정리 전후 커밋 매핑·브랜치·태그를 비교하고, 전체 도달 가능한 이력에서 8개 경로와 원본 blob이 남지 않았는지 검사합니다. 필터 범위 외 게임 파일의 각 트리 내용도 비교합니다. 최신 트리를 임포트해 동일한 UI·저장·화면 검사를 반복합니다.
7. **원격 반영은 별도 승인:** 도구가 origin을 제거할 수 있으므로 재설정 전에 주소를 확인합니다. 같은 저장소에 승인된 브랜치·태그만 갱신하며, 가능한 범위에서 기록해 둔 SHA를 조건으로 한 force-with-lease를 사용합니다. 브랜치 보호·다른 브랜치의 잔존 이력을 확인합니다. 로컬 특수 참조나 백업 참조까지 무심코 보내는 `push --mirror`는 사용하지 않습니다.
8. **다른 컴퓨터 정리:** 기존 Mac/Windows 복사본에서 예전 이력을 단순 merge/push하면 재유입될 수 있습니다. 미커밋 작업을 보존한 뒤 정리된 이력을 새로 복제해 필요한 변경만 이식합니다.

## GitHub 잔존 가능성과 최종 확인

이력 재작성·강제 Push 이후에도 PR 내부 참조, 캐시된 이전 SHA 화면, 포크·다른 컴퓨터의 clone, 릴리스 첨부물 등에 파일이 남을 수 있습니다. 로컬 `git log`가 비었다는 것만으로 GitHub에서 완전히 제거됐다고 판단하면 안 됩니다.

같은 저장소의 모든 공개 대상 브랜치·태그와 새 clone을 검사하고, 이전 commit/blob 접근·PR·포크·첨부물 잔존 여부를 확인해야 합니다. GitHub Support 문의가 필요할 수 있으나 공식 안내상 비민감 정보 삭제 지원은 보장되지 않습니다. EmojiOne 라이선스 문제를 인증정보 유출과 동일하게 취급해 삭제를 보장받을 수 있다고 가정하지 않습니다.

GitHub 공식 영향·잔존 안내: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository

현재 작업에서는 commit, push, force push, 이력 재작성, 저장소 Public 전환을 수행하지 않았습니다. 현재 파일 제거만으로 Public 전환 가능 판정을 내리지 않습니다.
