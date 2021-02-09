const gulp = require("gulp");
const del = require("del");
const ts = require('gulp-typescript');
const sourcemaps = require('gulp-sourcemaps');
const pathResolver = require('gulp-typescript-path-resolver');

const tsProject = ts.createProject("tsconfig.json")

function clean() {
    return del(["wwwroot/js/**"]);
}

function buildTs() {
    return tsProject.src()
        .pipe(sourcemaps.init())
        .pipe(tsProject())
        .pipe(pathResolver.tsPathResolver(tsProject.config.compilerOptions, {}))
        .pipe(sourcemaps.write())
        .pipe(gulp.dest(tsProject.config.compilerOptions.outDir));
}

exports.default = gulp.series(clean, buildTs);
exports.buildTs = buildTs;
exports.clean = clean;
