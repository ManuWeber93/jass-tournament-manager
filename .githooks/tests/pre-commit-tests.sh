#!/usr/bin/env bash
set -euo pipefail

hook=$(cd "$(dirname "$0")/.." && pwd)/pre-commit
temporary_directory=$(mktemp -d)
trap 'rm -rf "$temporary_directory"' EXIT
export GIT_CONFIG_NOSYSTEM=1
export GIT_CONFIG_GLOBAL=/dev/null
# Avoid inheriting the caller's index or repository when run from another hook.
unset GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE GIT_COMMON_DIR
mkdir -p "$temporary_directory/bin" "$temporary_directory/repository"
export FORMAT_LOG="$temporary_directory/formatter.log"
export FORMAT_MODE=clean
export PATH="$temporary_directory/bin:$PATH"

cat > "$temporary_directory/bin/dotnet" <<'FORMATTER'
#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' "$@" > "$FORMAT_LOG"
[[ "$FORMAT_MODE" != fail ]] || exit 42
if [[ "$FORMAT_MODE" == fix ]]; then
    shift 4
    for path in "$@"; do
        printf '// formatted\n' >> "$path"
    done
fi
FORMATTER
chmod +x "$temporary_directory/bin/dotnet"
cd "$temporary_directory/repository"
git init -q
git config user.name 'Hook Tests'
git config user.email 'hook-tests@example.invalid'
git config core.autocrlf false
mkdir -p .githooks src/Backend src/App
cp "$hook" .githooks/pre-commit
chmod +x .githooks/pre-commit
printf '<Solution><Project Path="src/Backend/Backend.csproj" /></Solution>' > JassTournamentManager.Backend.slnx
printf '// original\n' > 'src/Backend/File with spaces.cs'
printf '// app\n' > src/App/App.cs
printf 'Initial documentation\n' > README.md
git add .
git commit -qm 'Initial fixture'
git config core.hooksPath .githooks

assert_no_formatter() {
    [[ ! -f "$FORMAT_LOG" ]] || { echo 'Unexpected formatter invocation' >&2; exit 1; }
}

reset_fixture() {
    git reset --hard -q HEAD
    rm -f "$FORMAT_LOG"
    export FORMAT_MODE=clean
}

expect_commit_failure() {
    local before
    before=$(git rev-parse HEAD)
    if git commit -qm 'Must be blocked'; then
        echo 'Commit unexpectedly succeeded' >&2
        exit 1
    fi
    [[ "$(git rev-parse HEAD)" == "$before" ]]
}

printf 'Updated documentation\n' >> README.md
git add README.md
git commit -qm 'Documentation only'
assert_no_formatter
echo 'PASS: No relevant files skips formatting'

printf '// change\n' >> 'src/Backend/File with spaces.cs'
git add 'src/Backend/File with spaces.cs'
git commit -qm 'Clean backend file'
grep -Fxq 'src/Backend/File with spaces.cs' "$FORMAT_LOG"
[[ $(wc -l < "$FORMAT_LOG") -eq 5 ]]
echo 'PASS: Formatted file and path with spaces commit successfully'
reset_fixture

printf '// staged\n' >> 'src/Backend/File with spaces.cs'
git add 'src/Backend/File with spaces.cs'
index_before=$(git write-tree)
export FORMAT_MODE=fix
expect_commit_failure
[[ "$(git write-tree)" == "$index_before" ]]
grep -q '// formatted' 'src/Backend/File with spaces.cs'
echo 'PASS: Corrections stop the commit without changing the index'
reset_fixture

printf '// staged\n' >> 'src/Backend/File with spaces.cs'
git add 'src/Backend/File with spaces.cs'
printf '// unstaged\n' >> 'src/Backend/File with spaces.cs'
index_before=$(git write-tree)
worktree_before=$(git hash-object 'src/Backend/File with spaces.cs')
expect_commit_failure
assert_no_formatter
[[ "$(git write-tree)" == "$index_before" ]]
[[ "$(git hash-object 'src/Backend/File with spaces.cs')" == "$worktree_before" ]]
echo 'PASS: Partially staged files remain untouched'
reset_fixture

printf '// staged\n' >> 'src/Backend/File with spaces.cs'
git add 'src/Backend/File with spaces.cs'
export FORMAT_MODE=fail
expect_commit_failure
[[ -f "$FORMAT_LOG" ]]
echo 'PASS: Formatter failures block commits'
reset_fixture

mkdir -p src/Backend/Persistence/Migrations
printf '// generated\n' > src/Backend/Persistence/Migrations/Migration.cs
printf '// app change\n' >> src/App/App.cs
git add src
git commit -qm 'Excluded files only'
assert_no_formatter
echo 'PASS: App files and generated migrations are excluded'

echo 'All pre-commit hook tests passed.'